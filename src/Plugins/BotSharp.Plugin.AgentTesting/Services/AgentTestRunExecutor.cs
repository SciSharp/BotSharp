using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Plugin.AgentTesting.Repositories;

namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Run-level orchestration: turns an already-created AgentTestRun into serial execution of every
/// enabled case in its suite.
///
/// This class knows nothing about DI scopes -- it calls the single ICaseRunner instance handed to
/// its constructor, and the loop below decides how often and when. "A fresh DI scope per case" is
/// not done here; AgentTestRunQueue achieves it by injecting an ICaseRunner decorator that wraps
/// IServiceScopeFactory/IServiceProvider (see AgentTestRunQueue.ScopedCaseRunner). That decorator's
/// RunAsync opens a new scope on every call -- that is, per case -- resolves the real
/// AgentTestCaseRunner from it, and disposes the scope when the case finishes. As a result BotSharp
/// scoped services such as IConversationService, IConversationStateService and
/// TestMockExecutorProvider are never the same instance across two cases of one run. Keeping scope
/// creation out of this class is also what lets a DI-unaware DelegatingCaseRunner unit-test the
/// orchestration directly.
///
/// One field, CancelRequested, has a writer outside this class (POST .../runs/{id}/cancel), which
/// makes it the only field a whole-document ReplaceOneAsync could revert to a stale value --
/// TotalCount/PassedCount/... are written by this class alone. Hence the re-read AFTER each case
/// rather than before: the object it returns becomes the `run` this method then mutates and
/// persists, so both "should the next case run" and "will this write clobber an external one" are
/// decided from the state as of the moment that case finished, not from the copy read at the very
/// top and never refreshed. One read covers both questions, so no separate check is needed before
/// it.
/// </summary>
public class AgentTestRunExecutor
{
    private readonly IAgentTestRepository _repo;
    private readonly ICaseRunner _caseRunner;
    private readonly ILlmProviderService? _llmProviders;
    private readonly ILogger<AgentTestRunExecutor> _logger;

    public AgentTestRunExecutor(
        IAgentTestRepository repo,
        ICaseRunner caseRunner,
        ILogger<AgentTestRunExecutor> logger,
        ILlmProviderService? llmProviders = null)
    {
        _repo = repo;
        // Optional so the run-orchestration tests can construct an executor without a provider
        // registry. Without it the pricing snapshot is simply absent, which reads as "unknown" rather
        // than as "free".
        _llmProviders = llmProviders;
        _caseRunner = caseRunner;
        _logger = logger;
    }

    public async Task ExecuteAsync(string runId, CancellationToken ct)
    {
        var run = await _repo.GetRunAsync(runId);
        if (run == null)
        {
            _logger.LogError("Agent test run {RunId} was not found; nothing to execute.", runId);
            return;
        }

        var suite = await _repo.GetSuiteAsync(run.SuiteId);
        if (suite == null)
        {
            _logger.LogError(
                "Agent test run {RunId} references suite {SuiteId}, which no longer exists.",
                runId, run.SuiteId);
            run.Status = AgentTestStatus.Error;
            run.Error = $"The suite this run belongs to ({run.SuiteId}) no longer exists.";
            run.StartedAt ??= DateTime.UtcNow;
            run.CompletedAt = DateTime.UtcNow;
            await _repo.UpdateRunAsync(run);
            return;
        }

        // The trigger endpoint (POST .../suites/{id}/run) is the primary place a disabled suite
        // gets rejected -- a 400, before a run row even exists. This is defense-in-depth for the
        // race where a suite is disabled AFTER a run was already queued: same shape as the
        // suite-no-longer-exists branch just above, since "the suite this run belongs to says
        // don't run me" is the same kind of infrastructure-level stop.
        if (!suite.Enabled)
        {
            _logger.LogError(
                "Agent test run {RunId} references suite {SuiteId}, which is disabled.",
                runId, run.SuiteId);
            run.Status = AgentTestStatus.Error;
            run.Error = "The suite was disabled after this run was queued, so nothing ran.";
            run.StartedAt ??= DateTime.UtcNow;
            run.CompletedAt = DateTime.UtcNow;
            await _repo.UpdateRunAsync(run);
            return;
        }

        var cases = await _repo.ListCasesAsync(run.SuiteId);
        var enabledCases = cases.Where(c => c.Enabled).ToList();

        // A caller-selected subset (POST .../run's optional caseIds -- e.g. "re-run only the
        // cases that just failed") narrows the enabled set further. Null/empty means every
        // enabled case, unchanged from before this field existed.
        if (run.CaseIds is { Count: > 0 })
        {
            var allowed = new HashSet<string>(run.CaseIds, StringComparer.Ordinal);
            enabledCases = enabledCases.Where(c => allowed.Contains(c.Id)).ToList();

            if (enabledCases.Count == 0)
            {
                // Same "nothing executed, reports green" defect AgentTestCaseRunner already
                // guards for a case with zero turns: FailedCount == 0 && ErrorCount == 0 below is
                // vacuously true when the loop never runs at all, which is exactly what happens
                // when every id named by a non-empty CaseIds filter is unknown or disabled in
                // this suite. End the run as an infrastructure Error instead of a false Passed.
                _logger.LogError(
                    "Agent test run {RunId} named {CaseIdCount} case id(s) via CaseIds, but none "
                    + "of them matched an enabled case in suite {SuiteId}.",
                    runId, run.CaseIds.Count, run.SuiteId);
                run.Status = AgentTestStatus.Error;
                run.Error =
                    $"None of the {run.CaseIds.Count} selected case(s) could run: each one is either "
                    + "disabled or no longer in this suite. Enable them and run again.";
                run.StartedAt ??= DateTime.UtcNow;
                run.CompletedAt = DateTime.UtcNow;
                await _repo.UpdateRunAsync(run);
                return;
            }
        }

        run.Status = AgentTestStatus.Running;
        run.StartedAt = DateTime.UtcNow;
        run.TotalCount = 0;
        run.PassedCount = 0;
        run.FailedCount = 0;
        run.ErrorCount = 0;
        await _repo.UpdateRunAsync(run);

        var cancelled = false;

        // The model dimension. An absent/empty Models list means "one pass, using each agent's own
        // LlmConfig" -- byte-for-byte the behavior from before multi-model existed, which is what
        // keeps every historical run document and every caller that omits the field working.
        // Otherwise each enabled case runs once per model, so one run yields a case x model grid.
        //
        // Case-major order (case1/modelA, case1/modelB, case2/modelA, ...) so that a long run shows
        // a complete comparison for the first case early instead of only after the first model has
        // swept the whole suite.
        var models = run.Models is { Count: > 0 }
            ? run.Models.Cast<TestModel?>().ToList()
            : [null];

        var workItems = enabledCases
            .SelectMany(testCase => models.Select(model => (Case: testCase, Model: model)))
            .ToList();

        foreach (var (testCase, model) in workItems)
        {
            if (ct.IsCancellationRequested)
            {
                // The host itself is shutting down (BackgroundService's stoppingToken), not a
                // user-requested cancel of THIS run. Leave the row as Running rather than racing
                // the shutdown grace period to persist a different terminal status here -- the
                // queue's own startup reconciliation step sweeps any Running row a killed process
                // left behind into Error the next time it boots.
                return;
            }

            // `run` here is either the initial load (for the first case) or the post-case read
            // from the PREVIOUS iteration (see below) -- either way it is the freshest state this
            // method has seen, so this check can never miss a cancel that arrived any time up to
            // "the moment the previous case finished."
            if (run.CancelRequested)
            {
                cancelled = true;
                break;
            }

            AgentTestCaseResult result;
            try
            {
                result = await _caseRunner.RunAsync(suite, testCase, runId, model, ct);
            }
            catch (Exception ex)
            {
                // A single case crashing must not abort the run -- record it as Error and move on
                // to the next (case, model) pair. One model blowing up (a bad deployment name, a
                // revoked key) must not cost the comparison the other models' results.
                _logger.LogError(
                    ex, "Agent test case {CaseId} crashed under test run {RunId} with model {Model}.",
                    testCase.Id, runId, model?.ToString() ?? "<agent default>");
                result = new AgentTestCaseResult
                {
                    RunId = runId,
                    CaseId = testCase.Id,
                    CaseName = testCase.Name,
                    // Same reason the runner stamps these up front: an unattributed row is dead
                    // weight in a grid keyed by model.
                    Provider = model?.Provider,
                    Model = model?.Model,
                    CaseType = testCase.CaseType,
                    Status = AgentTestStatus.Error,
                    Error = ex.Message
                };
            }

            await _repo.AddCaseResultAsync(result);

            // Re-read AFTER the case ran, not before. This is what picks up a concurrent
            // POST /runs/{id}/cancel that landed WHILE the case was executing -- both for the
            // NEXT iteration's check above, and so the persist a few lines down (a whole-document
            // ReplaceOneAsync) never overwrites that external write with a stale
            // CancelRequested=false. `run` becomes this fresh object for the remainder of the
            // method (including the terminal write after the loop, if this was the last case) --
            // that's what keeps a cancel that arrives during the very last case's own execution
            // from being silently erased, with no extra read needed after the loop.
            run = await _repo.GetRunAsync(runId) ?? run;

            run.TotalCount++;
            TallyRoutingAccuracy(run, result);
            switch (result.Status)
            {
                case AgentTestStatus.Passed:
                    run.PassedCount++;
                    break;
                case AgentTestStatus.Failed:
                    run.FailedCount++;
                    break;
                default:
                    // Error, Cancelled (e.g. a per-case timeout inside the real case runner), or
                    // any other non-Passed/Failed status all count against ErrorCount -- AgentTestRun
                    // has no separate CancelledCount field, and a per-case timeout is an
                    // infrastructure failure for THAT case, not a graceful run-level cancellation.
                    run.ErrorCount++;
                    break;
            }

            // Real-time accumulation, per case -- not just at the very end. Without this, GET
            // /agent-test/runs/{id} shows 0/0/0/0 for the run's entire duration, and a process
            // death mid-run leaves the startup sweep's Error row claiming zero cases ran even
            // though N AgentTestCaseResult rows already exist for it.
            await _repo.UpdateRunAsync(run);
        }

        run.Status = cancelled
            ? AgentTestStatus.Cancelled
            : run.FailedCount == 0 && run.ErrorCount == 0
                ? AgentTestStatus.Passed
                : AgentTestStatus.Failed;
        run.CompletedAt = DateTime.UtcNow;

        await SummarisePerformanceAsync(run);

        await _repo.UpdateRunAsync(run);
    }

    /// <summary>
    /// Fills in the run's per-model latency, token and cost figures, plus the pricing that produced
    /// the cost.
    ///
    /// Once, at the end, reading the results back -- not accumulated per case like the counts are.
    /// A percentile is not incrementally computable: it needs every value at once, and keeping the
    /// whole list on the run document to update it in place would store the same numbers twice.
    /// </summary>
    private async Task SummarisePerformanceAsync(AgentTestRun run)
    {
        List<AgentTestCaseResult> results;
        try
        {
            results = await _repo.ListCaseResultsAsync(run.Id);
        }
        catch (Exception ex)
        {
            // Reporting figures must never cost the run its terminal status: the case results are
            // already stored and are the source of truth, so a failure here loses a summary, not
            // data.
            _logger.LogWarning(ex, "Could not summarise performance for agent test run {RunId}.", run.Id);
            return;
        }

        run.PerformanceSummaries = results
            .GroupBy(r => (r.Provider, r.Model))
            .Select(group => BuildSummary(group.Key.Provider, group.Key.Model, group.ToList()))
            .ToList();

        run.ModelPricing = SnapshotPricing(run);
    }

    private static PerformanceSummary BuildSummary(
        string? provider, string? model, List<AgentTestCaseResult> results)
    {
        // Only cases that reached the model. An Error case that died before its first turn has a
        // ModelDurationMs of zero, and letting those into the percentile makes a run that mostly
        // crashed look like the fastest one on record.
        var latencies = results
            .Where(r => r.ModelDurationMs > 0)
            .Select(r => r.ModelDurationMs)
            .OrderBy(ms => ms)
            .ToList();

        return new PerformanceSummary
        {
            Provider = provider,
            Model = model,
            CaseCount = latencies.Count,
            LatencyP50Ms = Percentile(latencies, 0.50),
            LatencyP95Ms = Percentile(latencies, 0.95),
            // Tokens and cost come from EVERY result, unlike latency: a case that errored still spent
            // whatever it spent, and hiding that would understate the run's real cost.
            TotalTokens = results.Sum(r => r.TotalTokens),
            TotalCost = results.Sum(r => r.Cost)
        };
    }

    /// <summary>
    /// Nearest-rank percentile over an already-sorted list: the value at ceil(p * n) - 1, so P95 of
    /// twenty samples is the 19th and P50 of an even count is the lower of the two middle values.
    ///
    /// Deliberately not interpolated. An interpolated P95 returns a duration no case actually took,
    /// which is indefensible when someone asks which case was the slow one -- and with the handful of
    /// cases a real suite starts with, interpolation invents most of the answer.
    /// </summary>
    private static long Percentile(List<long> sorted, double percentile)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        var rank = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Clamp(rank, 0, sorted.Count - 1)];
    }

    /// <summary>
    /// The unit costs in force for each model this run swept. Without them a cost figure cannot be
    /// compared with any other run's: a provider price change would show up as a cost regression with
    /// nothing to point at.
    /// </summary>
    private List<ModelPricingSnapshot> SnapshotPricing(AgentTestRun run)
    {
        if (_llmProviders == null || run.Models is not { Count: > 0 })
        {
            // No models named means each agent ran on its own LlmConfig, which this method cannot
            // resolve without reading every agent involved. Left empty rather than guessed.
            return [];
        }

        return run.Models
            .Select(m =>
            {
                LlmModelSetting? setting = null;
                try
                {
                    setting = _llmProviders.GetSetting(m.Provider, m.Model);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Could not read pricing for {Provider}/{Model} while summarising run {RunId}.",
                        m.Provider, m.Model, run.Id);
                }

                return new ModelPricingSnapshot
                {
                    Provider = m.Provider,
                    Model = m.Model,
                    TextInputCost = setting?.Cost?.TextInputCost,
                    TextOutputCost = setting?.Cost?.TextOutputCost
                };
            })
            .ToList();
    }

    /// <summary>
    /// Folds one case result into the run's per-model routing accuracy. Only Routing cases count:
    /// the evaluation framework gates routing accuracy separately from the agent pass rate, so
    /// mixing an agent case into this figure would make the gate unreadable.
    ///
    /// Rows are keyed by (provider, model) and created on first sight, which keeps this correct for
    /// a run that sweeps no models at all -- that produces the single (null, null) row.
    /// </summary>
    private static void TallyRoutingAccuracy(AgentTestRun run, AgentTestCaseResult result)
    {
        if (!string.Equals(result.CaseType, CaseTypes.Routing, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var row = run.RoutingAccuracies.FirstOrDefault(a =>
            string.Equals(a.Provider, result.Provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Model, result.Model, StringComparison.OrdinalIgnoreCase));

        if (row == null)
        {
            row = new RoutingAccuracy { Provider = result.Provider, Model = result.Model };
            run.RoutingAccuracies.Add(row);
        }

        row.CaseCount++;
        if (string.Equals(result.Status, AgentTestStatus.Passed, StringComparison.Ordinal))
        {
            row.PassedCount++;
        }
    }

}
