using BotSharp.Plugin.AgentTesting.Repositories;

namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Run 层的编排：把一个已创建的 AgentTestRun 变成对其 Suite 下每条启用用例的串行执行。
///
/// 这个类本身对 DI scope 一无所知——它只调用构造时给定的那一个 ICaseRunner 实例，调几次、
/// 什么时候调完全由下面的 foreach 决定。"每个用例一个新 DI scope" 这件事不是这里做的，是
/// AgentTestRunQueue 通过给这里注入一个包了 IServiceScopeFactory/IServiceProvider 的
/// ICaseRunner 装饰器做到的（见 AgentTestRunQueue.ScopedCaseRunner）：那个装饰器的
/// RunAsync 每次被调用（也就是每一个 case）都会自己开一个新 scope、从里面解析真正的
/// AgentTestCaseRunner、跑完就释放这个 scope。这样一来，IConversationService/
/// IConversationStateService/TestMockExecutorProvider 这些 BotSharp scoped 服务，
/// 在同一个 Run 里的两个 case 之间永远不是同一个实例——不靠这个类自己创建 scope，
/// 单元测试才能用一个不知道 DI 是什么的 DelegatingCaseRunner 直接测编排逻辑。
///
/// fix round 1 记录：CancelRequested 这一个字段有 THIS CLASS 自己以外的写者（POST
/// .../runs/{id}/cancel），是唯一一个"整文档 ReplaceOneAsync 可能把外部刚写进去的值覆盖回旧值"
/// 的字段——TotalCount/PassedCount/... 只有这个类自己写，不存在这个问题。修法是：每条 case
/// 跑完之后（不是跑之前）都重新 GetRunAsync 一次，把返回的对象整个接过来当作接下来要
/// 修改/持久化的 `run`——这样"下一条 case 该不该跑"的判据、以及这次持久化会不会把外部写
/// 覆盖掉，用的都是"这条 case 刚跑完那一刻"的最新状态，而不是方法最开头读到的那份、从头到尾
/// 再也没刷新过的旧对象。这一步只需要一次读，不需要在读之前再单独查一次——因为"该不该继续跑
/// 下一条"和"这次持久化别把外部写盖掉"用的是同一份最新读。
/// </summary>
public class AgentTestRunExecutor
{
    private readonly IAgentTestRepository _repo;
    private readonly ICaseRunner _caseRunner;
    private readonly ILogger<AgentTestRunExecutor> _logger;

    public AgentTestRunExecutor(
        IAgentTestRepository repo,
        ICaseRunner caseRunner,
        ILogger<AgentTestRunExecutor> logger)
    {
        _repo = repo;
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
        await _repo.UpdateRunAsync(run);
    }
}
