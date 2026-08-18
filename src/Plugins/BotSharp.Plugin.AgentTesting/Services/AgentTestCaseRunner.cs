using System.Diagnostics;
using BotSharp.Plugin.AgentTesting.Runtime;

namespace BotSharp.Plugin.AgentTesting.Services;

public class AgentTestCaseRunner : ICaseRunner
{
    private readonly IAgentTestRunRegistry _registry;
    private readonly IAgentConversationDriver _driver;
    private readonly ILogger<AgentTestCaseRunner> _logger;

    public AgentTestCaseRunner(
        IAgentTestRunRegistry registry,
        IAgentConversationDriver driver,
        ILogger<AgentTestCaseRunner> logger)
    {
        _registry = registry;
        _driver = driver;
        _logger = logger;
    }

    public async Task<AgentTestCaseResult> RunAsync(
        AgentTestSuite suite,
        AgentTestCase testCase,
        string runId,
        TestModel? model,
        CancellationToken ct)
    {
        var conversationId = Guid.NewGuid().ToString();
        var result = new AgentTestCaseResult
        {
            RunId = runId,
            CaseId = testCase.Id,
            CaseName = testCase.Name,
            ConversationId = conversationId,
            // Stamped up front so even the early-return paths below (no turns, canary failure,
            // timeout) still say which model they were meant to run under -- a result that cannot
            // be attributed to a model is useless in a comparison run.
            Provider = model?.Provider,
            Model = model?.Model
        };

        // Turns.SelectMany(...).Concat(caseAssertions).All(a => a.Passed) is vacuously true on an
        // empty sequence, so a case with no turns would otherwise execute nothing and still report
        // Passed. Catch it here, before the driver is touched at all: no PrepareAsync, no canary,
        // no conversation ever opened for a case that was never going to run anything.
        if (testCase.Turns.Count == 0)
        {
            result.Status = AgentTestStatus.Error;
            result.Error = "the case has no turns";
            return result;
        }

        var active = new ActiveTestRun
        {
            ConversationId = conversationId,
            CaseId = testCase.Id,
            ModelOverride = model,
            Mocks = testCase.Mocks,
            UnmockedToolPolicy = testCase.UnmockedToolPolicy,
            AllowedFunctions = BuildAllowList(suite),
            ForceBlockedFunctions = new HashSet<string>(suite.ForceBlockedFunctions, StringComparer.OrdinalIgnoreCase)
        };

        var stopwatch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, suite.CaseTimeoutSeconds)));

        // Fix round 1, Finding 1. BotSharp's SendMessage/InvokeFunction have no CancellationToken
        // of their own, so a driver call can at best be RACED against `timeout` (see
        // AwaitOrHandOffAsync below) -- it can never actually be aborted. When OUR OWN timeout is
        // what fired, the real call is very likely still running server-side, still walking
        // through BotSharp's routing/tool-invocation loop. Unregistering the conversation right
        // now (the old, unconditional `finally` behavior) would remove the only thing making
        // TestMockExecutorProvider intercept its NEXT function call -- the mock seam disappears
        // out from under a still-running case, and its next tool call falls through to the REAL
        // implementation (a real phone call, a real email). So on that path specifically, cleanup
        // is handed off to a continuation on the raw driver task, and the `finally` below must NOT
        // unregister inline while that hand-off is pending.
        var orphanHandedOff = false;

        async Task<T> AwaitOrHandOffAsync<T>(Task<T> driverTask)
        {
            try
            {
                return await driverTask.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                orphanHandedOff = true;
                _ = driverTask.ContinueWith(t =>
                {
                    // Observe the antecedent's exception, if any, before anything else: an
                    // orphaned real driver call that later throws (e.g. the underlying BotSharp
                    // SendMessage/InvokeFunction call itself fails after we already gave up
                    // waiting on it) previously vanished completely -- no case result reflects it
                    // (the case already recorded "timed out" and returned), and the exception was
                    // never even observed, so at best it surfaces as an UnobservedTaskException at
                    // GC. This is the cheapest available diagnostic for the single highest-risk
                    // unverified property in this whole feature: a real call still running after
                    // RunAsync itself has already returned.
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception,
                            "Orphaned agent test driver call for case {CaseId} (conversation {ConversationId}) "
                            + "faulted after the case's own timeout had already elapsed. The case result already "
                            + "recorded a timeout; this log is diagnostic-only.",
                            testCase.Id, conversationId);
                    }

                    try { _registry.Unregister(conversationId); }
                    catch { /* best-effort cleanup of an orphaned call; nothing else to do here */ }
                }, TaskScheduler.Default);
                throw;
            }
        }

        _registry.Register(active);
        try
        {
            await _driver.PrepareAsync(conversationId, suite.AgentId, testCase.InitialStates);

            // Prove the seam is live first. A dead seam means mocking silently does nothing and
            // real tools execute, so this has to happen before a single user message is sent.
            //
            // The verdict comes only from the driver's return value: the real driver derives that
            // bool from whether the canary call's content was replaced with 'canary', and that
            // content only ever appears when MockFunctionExecutor took over. Also checking
            // active.CanaryIntercepted would look stricter but checks the same fact twice, and it
            // would stop a fake driver from unit-testing the orchestration at all -- a fake driver
            // has no ActiveTestRun and can never set that flag.
            if (!await AwaitOrHandOffAsync(_driver.RunCanaryAsync(conversationId, suite.AgentId, timeout.Token)))
            {
                result.Status = AgentTestStatus.Error;
                result.Error = "the mock seam is not live: IFunctionExecutorProvider was not consulted. "
                             + "Check that the build uses DebugBrain.sln (BotSharp from source) and that "
                             + "BotSharp.Plugin.AgentTesting is listed in PluginLoader:Assemblies.";
                return result;
            }

            var fatalStop = false;
            foreach (var turn in testCase.Turns.OrderBy(t => t.Index))
            {
                if (fatalStop) break;

                active.CurrentTurnIndex = turn.Index;
                var output = await AwaitOrHandOffAsync(
                    _driver.SendAsync(conversationId, suite.AgentId, turn.UserMessage, timeout.Token));

                var turnResult = new TurnResult
                {
                    Index = turn.Index,
                    UserMessage = turn.UserMessage,
                    Output = output
                };

                var turnContext = new AssertionContext
                {
                    Output = output,
                    ToolCalls = active.ObservedCalls.Where(c => c.TurnIndex == turn.Index).ToList(),
                    States = await _driver.ReadStatesAsync(conversationId),
                    RoutedToAgent = await _driver.ReadRoutedAgentNameAsync(conversationId)
                };

                foreach (var assertion in turn.Assertions)
                {
                    var evaluated = AssertionEvaluator.Evaluate(assertion, turnContext);
                    turnResult.Assertions.Add(evaluated);
                    if (!evaluated.Passed && assertion.Fatal)
                    {
                        fatalStop = true;
                    }
                }

                result.Turns.Add(turnResult);
            }

            var finalContext = new AssertionContext
            {
                Output = result.Turns.LastOrDefault()?.Output,
                ToolCalls = active.ObservedCalls,
                States = await _driver.ReadStatesAsync(conversationId),
                RoutedToAgent = await _driver.ReadRoutedAgentNameAsync(conversationId)
            };

            foreach (var assertion in testCase.Assertions)
            {
                result.Assertions.Add(AssertionEvaluator.Evaluate(assertion, finalContext));
            }

            result.ObservedToolCalls = active.ObservedCalls.ToList();

            AddBlockedToolFailure(result);

            var allAssertions = result.Turns.SelectMany(t => t.Assertions).Concat(result.Assertions);
            result.Status = allAssertions.All(a => a.Passed) ? AgentTestStatus.Passed : AgentTestStatus.Failed;
        }
        // Fix round 1, Finding 3. The old `when (!ct.IsCancellationRequested)` guard mislabeled ANY
        // OperationCanceledException that wasn't the caller's own cancellation as "the case timed
        // out" -- including, say, an HttpClient timeout raised deep inside a passthrough tool call,
        // which has nothing to do with `timeout`/CaseTimeoutSeconds at all. Tightened to require
        // OUR OWN timeout to actually be the one that fired; anything else (`ct` cancelled, or some
        // unrelated cancellation) falls through to the clauses below, which record what really
        // happened instead of a misleading "timed out".
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            // This case's own timeout, not a cancellation of the whole run. "Could not run" and
            // "ran and came out wrong" have to stay distinguishable.
            result.Status = AgentTestStatus.Error;
            result.Error = $"the case timed out after {suite.CaseTimeoutSeconds}s";
            result.ObservedToolCalls = active.ObservedCalls.ToList();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            result.Status = AgentTestStatus.Cancelled;
            result.ObservedToolCalls = active.ObservedCalls.ToList();
        }
        catch (Exception ex)
        {
            // Reaches here for any OperationCanceledException that was neither our own timeout nor
            // the caller's cancellation too (e.g. an unrelated cancellation raised inside a
            // passthrough tool call), as well as every other exception -- both get the real
            // message instead of being folded into "timed out"/"cancelled".
            _logger.LogError(ex, "Agent test case {CaseId} crashed.", testCase.Id);
            result.Status = AgentTestStatus.Error;
            result.Error = ex.Message;
            result.ObservedToolCalls = active.ObservedCalls.ToList();
        }
        finally
        {
            // Leaking a registry entry would mean every later tool call on that conversationId is
            // still intercepted as a test -- unless a timeout already handed the removal to the
            // ContinueWith above, in which case removing it here early is exactly what must not
            // happen.
            if (!orphanHandedOff)
            {
                _registry.Unregister(conversationId);
            }
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// A blocked tool call fails the case, as a synthetic case-level assertion.
    ///
    /// Blocking is the mock seam working correctly -- the agent reached for a tool this case does
    /// not mock, and executing it for real could have sent an email or created a work order. But
    /// the block also truncates that turn (StopCompletion), so everything the agent would have done
    /// afterwards never happened and every later assertion is evaluated against a conversation that
    /// stopped early. Reporting Passed there is the "executed nothing, reports green" defect this
    /// harness guards against everywhere else (a case with no turns, a dead canary, a CaseIds filter
    /// that matched nothing).
    ///
    /// Modelled as an assertion rather than as result.Error so it renders in the ordinary assertion
    /// table with expected/actual, and so the existing all-assertions-passed rule decides the status
    /// with no special case. Error stays reserved for "the harness itself did not work", which is
    /// the opposite of what happened here.
    /// </summary>
    private static void AddBlockedToolFailure(AgentTestCaseResult result)
    {
        var blocked = result.ObservedToolCalls
            .Where(c => string.Equals(c.Outcome, "Blocked", StringComparison.Ordinal))
            .ToList();

        if (blocked.Count == 0)
        {
            return;
        }

        var named = string.Join(", ", blocked
            .Select(c => $"{c.FunctionName} (turn {c.TurnIndex + 1})")
            .Distinct(StringComparer.Ordinal));

        result.Assertions.Add(new AssertionResult
        {
            Type = AssertionTypes.NoBlockedTools,
            Target = null,
            Expected = "every tool the agent calls is mocked by this case",
            Actual = named,
            Passed = false,
            Message = blocked.Count == 1
                ? "The agent called a tool this case does not mock, so it was blocked and the turn "
                  + "stopped there. Add a mock for it, or change the case so the agent does not need it."
                : $"The agent called {blocked.Count} tools this case does not mock, so they were "
                  + "blocked and their turns stopped there. Add mocks for them, or change the case "
                  + "so the agent does not need them."
        });
    }

    private static HashSet<string> BuildAllowList(AgentTestSuite suite)
    {
        var allow = new HashSet<string>(ControlFlowFunctions.Default, StringComparer.OrdinalIgnoreCase);
        foreach (var extra in suite.ExtraAllowedFunctions)
        {
            allow.Add(extra);
        }
        return allow;
    }
}
