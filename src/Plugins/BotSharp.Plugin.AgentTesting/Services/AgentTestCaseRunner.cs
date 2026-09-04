using System.Diagnostics;
using BotSharp.Plugin.AgentTesting.Runtime;

namespace BotSharp.Plugin.AgentTesting.Services;

public class AgentTestCaseRunner : ICaseRunner
{
    private readonly IAgentTestRunRegistry _registry;
    private readonly IAgentConversationDriver _driver;
    private readonly IAgentTestJudge? _judge;
    private readonly ITokenStatistics? _tokens;
    private readonly ILogger<AgentTestCaseRunner> _logger;

    public AgentTestCaseRunner(
        IAgentTestRunRegistry registry,
        IAgentConversationDriver driver,
        ILogger<AgentTestCaseRunner> logger,
        IAgentTestJudge? judge = null,
        ITokenStatistics? tokens = null)
    {
        _registry = registry;
        _driver = driver;
        _logger = logger;
        // Optional so the orchestration tests can build a runner without one. ITokenStatistics is
        // registered scoped, and AgentTestRunQueue.ScopedCaseRunner opens a fresh scope per case, so
        // in production this is the same instance the conversation's completion provider reports
        // into -- which is what makes the delta below attributable to this case alone.
        _tokens = tokens;
        // Optional so the orchestration tests can construct a runner without a vendor. A null judge
        // is not a silent skip: EvaluateAsync turns an llmJudge assertion into the same Error as an
        // unreachable vendor, because a case whose quality assertion was never scored has an unknown
        // verdict, not a passing one.
        _judge = judge;
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
            Model = model?.Model,
            // Stamped up front like Provider/Model: aggregating routing accuracy separately from
            // agent pass rate has to work off the result rows alone, including the rows produced by
            // the early returns below.
            CaseType = testCase.CaseType
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

        // Which agent the conversation opens on. BotSharp dispatches on that agent's own type
        // (ConversationService.SendMessage: a Routing agent goes through RoutingService.InstructLoop
        // and can hand off, everything else through InstructDirect and cannot), so this one value is
        // what decides whether the router is part of what the case measures. The suite's agent stays
        // the default, so every case authored before EntryAgentId existed behaves exactly as before.
        var entryAgentId = string.IsNullOrWhiteSpace(testCase.EntryAgentId)
            ? suite.AgentId
            : testCase.EntryAgentId!;

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

        // Read as a delta, not an absolute: a scope that outlived an earlier case would otherwise
        // have this case billed for that one's tokens too. On a fresh scope the baseline is zero and
        // the delta is simply the final reading.
        var tokensBefore = _tokens?.Total ?? 0;
        var costBefore = _tokens?.AccumulatedCost ?? 0f;
        long modelDurationMs = 0;

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
            await _driver.PrepareAsync(conversationId, entryAgentId, testCase.InitialStates);

            // Authored history goes in before the canary and before any turn, so the model sees it
            // as the conversation's opening context. A short count means the write silently did
            // nothing (see IAgentConversationDriver.InjectHistoryAsync) -- that has to be an Error,
            // because a case running without the context it was written around would otherwise
            // report an ordinary pass or fail about a scenario that never existed.
            var injectedHistory = await AwaitOrHandOffAsync(
                _driver.InjectHistoryAsync(conversationId, entryAgentId, testCase.History));

            if (injectedHistory != testCase.History.Count)
            {
                result.Status = AgentTestStatus.Error;
                result.Error = $"only {injectedHistory} of {testCase.History.Count} history messages "
                             + "could be written to the conversation, so this case would have run "
                             + "without the context it was written around";
                return result;
            }

            // Authored history is not something the agent under test did, so it must not appear in
            // the chain. Read once here and used as the offset for both the per-turn slices and the
            // case-level chain, so the exclusion cannot drift between them.
            var historyAssistantMessages = testCase.History.Count == 0
                ? 0
                : (await _driver.ReadAssistantAgentSequenceAsync(conversationId)).Count;

            // Prove the seam is live first. A dead seam means mocking silently does nothing and
            // real tools execute, so this has to happen before a single user message is sent.
            //
            // The verdict comes only from the driver's return value: the real driver derives that
            // bool from whether the canary call's content was replaced with 'canary', and that
            // content only ever appears when MockFunctionExecutor took over. Also checking
            // active.CanaryIntercepted would look stricter but checks the same fact twice, and it
            // would stop a fake driver from unit-testing the orchestration at all -- a fake driver
            // has no ActiveTestRun and can never set that flag.
            if (!await AwaitOrHandOffAsync(_driver.RunCanaryAsync(conversationId, entryAgentId, timeout.Token)))
            {
                result.Status = AgentTestStatus.Error;
                result.Error = "the mock seam is not live: IFunctionExecutorProvider was not consulted. "
                             + "Check that the build uses DebugBrain.sln (BotSharp from source) and that "
                             + "BotSharp.Plugin.AgentTesting is listed in PluginLoader:Assemblies.";
                return result;
            }

            // How many assistant messages the chain has already accounted for. The driver hands
            // back the whole conversation on every read -- that is what the dialog store holds --
            // and only the runner knows where each turn started, so the per-turn slice is taken
            // here.
            var consumedAssistantMessages = historyAssistantMessages;

            var fatalStop = false;
            foreach (var turn in testCase.Turns.OrderBy(t => t.Index))
            {
                if (fatalStop) break;

                active.CurrentTurnIndex = turn.Index;
                // Timed around the agent call ONLY. The case's own DurationMs also covers the
                // canary, the mock lookups and the conversation reads, and on a fast case that
                // overhead is a big enough share to move a latency percentile.
                var turnTimer = Stopwatch.StartNew();
                var output = await AwaitOrHandOffAsync(
                    _driver.SendAsync(conversationId, entryAgentId, turn.UserMessage, timeout.Token));
                turnTimer.Stop();
                modelDurationMs += turnTimer.ElapsedMilliseconds;

                var agentSequence = await _driver.ReadAssistantAgentSequenceAsync(conversationId);
                var turnChain = CollapseConsecutiveRepeats(agentSequence.Skip(consumedAssistantMessages));
                consumedAssistantMessages = agentSequence.Count;

                var turnResult = new TurnResult
                {
                    Index = turn.Index,
                    UserMessage = turn.UserMessage,
                    Output = output,
                    ModelDurationMs = turnTimer.ElapsedMilliseconds,
                    // Names, not hops: a result is read by a person, and the ids are only needed
                    // while an assertion is being evaluated.
                    AgentChain = turnChain.Select(hop => hop.Name).ToList()
                };

                // This turn's slice, not the whole conversation: routedToAgent reads the chain's
                // last entry, and a turn-level context carrying the conversation's chain would let a
                // turn that produced no answer at all inherit the previous turn's agent and pass an
                // assertion about routing that never happened.
                var turnContext = new AssertionContext
                {
                    Output = output,
                    ToolCalls = active.ObservedCalls.Where(c => c.TurnIndex == turn.Index).ToList(),
                    States = await _driver.ReadStatesAsync(conversationId),
                    AgentChain = turnChain
                };

                foreach (var assertion in turn.Assertions)
                {
                    var evaluated = await EvaluateAsync(assertion, turnContext, suite, timeout.Token);
                    turnResult.Assertions.Add(evaluated);
                    if (!evaluated.Passed && assertion.Fatal)
                    {
                        fatalStop = true;
                    }
                }

                result.Turns.Add(turnResult);
            }

            var caseChain = CollapseConsecutiveRepeats(
                (await _driver.ReadAssistantAgentSequenceAsync(conversationId)).Skip(historyAssistantMessages));
            result.AgentChain = caseChain.Select(hop => hop.Name).ToList();

            var finalContext = new AssertionContext
            {
                Output = result.Turns.LastOrDefault()?.Output,
                ToolCalls = active.ObservedCalls,
                States = await _driver.ReadStatesAsync(conversationId),
                AgentChain = caseChain
            };

            foreach (var assertion in testCase.Assertions)
            {
                result.Assertions.Add(await EvaluateAsync(assertion, finalContext, suite, timeout.Token));
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
        catch (AgentTestJudgeUnavailableException ex)
        {
            // The judge never reached a verdict. That is Error, not Failed: a vendor timeout or an
            // unconfigured judge model says nothing about the agent under test, and reporting it as
            // a failing assertion would make provider noise indistinguishable from an agent
            // regression. Logged at warning, not error -- this is a configuration or vendor
            // condition, not a crash in the harness.
            _logger.LogWarning(ex, "Agent test case {CaseId} could not be judged.", testCase.Id);
            result.Status = AgentTestStatus.Error;
            result.Error = ex.Message;
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
            result.ModelDurationMs = modelDurationMs;

            // In the finally block so a timed-out or crashed case still reports what it spent. A run
            // that fell over having burned the budget is exactly the run whose cost matters.
            result.TotalTokens = Math.Max(0, (_tokens?.Total ?? 0) - tokensBefore);
            result.Cost = Math.Max(0, (_tokens?.AccumulatedCost ?? 0f) - costBefore);

            if (result.TotalTokens == 0 && result.Turns.Count > 0 && _tokens != null)
            {
                // Every turn calls the model, so zero tokens across completed turns means this
                // runner's ITokenStatistics is not the instance the completion provider reported
                // into. Logged rather than failed: usage accounting being wrong says nothing about
                // whether the agent behaved, and failing the case would report a metering problem as
                // an agent regression.
                _logger.LogWarning(
                    "Agent test case {CaseId} completed {TurnCount} turn(s) but measured zero tokens; "
                    + "token and cost figures for this run are not trustworthy.",
                    testCase.Id, result.Turns.Count);
            }
        }

        return result;
    }

    /// <summary>
    /// Consecutive duplicates removed, so ["A", "A", "B", "A"] becomes ["A", "B", "A"]. Only
    /// consecutive ones: a conversation that really went A -> B -> A did visit A twice, and
    /// flattening that to ["A", "B"] would hide the return hop from an ordered agentChain assertion.
    /// One agent emitting several messages in a row is not a hand-off and does collapse.
    /// </summary>
    private static List<AgentChainHop> CollapseConsecutiveRepeats(IEnumerable<AgentChainHop> hops)
    {
        var chain = new List<AgentChainHop>();
        foreach (var hop in hops)
        {
            // Compared by id, not name: two agents can share a display name, and collapsing those
            // together would hide a real hand-off.
            if (chain.Count == 0 || !string.Equals(chain[^1].Id, hop.Id, StringComparison.OrdinalIgnoreCase))
            {
                chain.Add(hop);
            }
        }

        return chain;
    }

    /// <summary>
    /// Evaluates one assertion. Everything except llmJudge goes to the pure, synchronous
    /// <see cref="AssertionEvaluator"/>; llmJudge needs a model call, so it goes to
    /// <see cref="IAgentTestJudge"/> instead. Keeping the split here rather than inside the evaluator
    /// is what lets every other assertion type stay reproducible and I/O-free.
    ///
    /// Any <see cref="AgentTestJudgeUnavailableException"/> propagates deliberately: the caller turns
    /// it into a case-level Error. Swallowing it into a failing assertion would report a vendor
    /// problem as an agent regression.
    /// </summary>
    private async Task<AssertionResult> EvaluateAsync(
        TestAssertion assertion,
        AssertionContext context,
        AgentTestSuite suite,
        CancellationToken ct)
    {
        if (!string.Equals(assertion.Type, AssertionTypes.LlmJudge, StringComparison.Ordinal))
        {
            return AssertionEvaluator.Evaluate(assertion, context);
        }

        if (_judge == null)
        {
            throw new AgentTestJudgeUnavailableException(
                "no IAgentTestJudge is registered, so llmJudge assertions cannot be scored");
        }

        return await _judge.JudgeAsync(assertion, context, suite, ct);
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
