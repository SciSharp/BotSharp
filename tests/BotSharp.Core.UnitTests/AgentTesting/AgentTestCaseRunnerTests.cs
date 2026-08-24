using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BotSharp.Plugin.AgentTesting.Runtime;
using BotSharp.Plugin.AgentTesting.Services;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// The runner turns one case into one real conversation. A fake driver is used here to unit-test the
/// orchestration itself: whether multiple turns are driven in order, whether a Fatal assertion really
/// aborts the remaining turns, whether a timeout records Error rather than Failed, and most
/// importantly the canary -- if the seam is not live (a build resolving an unpatched BotSharp
/// package, say) the case has to fail explicitly instead of running against real tools and
/// "passing".
/// </summary>
public class AgentTestCaseRunnerTests
{
    private sealed class FakeDriver : IAgentConversationDriver
    {
        public List<string> Sent { get; } = [];
        public Queue<string> Replies { get; } = new();
        public bool CanaryResult { get; set; } = true;
        public Dictionary<string, string?> States { get; } = new();
        public string? RoutedAgent { get; set; }
        public TimeSpan SendDelay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// The agents that answer each turn, one entry per SendAsync in call order. Lets a test model
        /// a hand-off inside a single turn (["Copilot", "WorkOrder"]), one agent answering several
        /// times in a row (["A", "A"]), or a turn that produced no answer at all ([]) -- none of
        /// which <see cref="RoutedAgent"/> can express. A turn with no entry here is answered by
        /// RoutedAgent, which is what leaves the pre-existing single-agent tests untouched.
        /// </summary>
        public List<string[]> AgentsPerTurn { get; } = [];

        /// <summary>
        /// The accumulated assistant-message sequence, exactly as the real driver reads it back out
        /// of the dialog store: never sliced per turn and never de-duplicated, because slicing and
        /// collapsing are the runner's job and that is what these tests are checking.
        /// </summary>
        private readonly List<AgentChainHop> _agentSequence = [];

        /// <summary>Everything InjectHistoryAsync was handed, in order.</summary>
        public List<TestHistoryMessage> InjectedHistory { get; } = [];

        /// <summary>
        /// Overrides what InjectHistoryAsync reports as written, to simulate the real failure mode:
        /// AppendConversationDialogs is an UpdateOne with no upsert, so it silently writes nothing
        /// when the dialog document is missing. Null reports every message as written.
        /// </summary>
        public int? HistoryWriteCount { get; set; }

        /// <summary>
        /// Agent the authored assistant history is attributed to. Set it to something the turns do
        /// NOT use to prove the history is excluded from the chain rather than merely collapsed into
        /// the first turn's entry.
        /// </summary>
        public string? HistoryAgentName { get; set; }

        /// <summary>
        /// Runs at the start of each SendAsync. Lets a test move an external counter -- the token
        /// meter -- during the case rather than only before or after it, which is the only way to
        /// exercise the delta.
        /// </summary>
        public Action? OnSend { get; set; }

        /// <summary>Which agent each call was made against, so a test can prove which one won.</summary>
        public string? PreparedAgentId { get; private set; }
        public string? CanaryAgentId { get; private set; }
        public List<string> SentAgentIds { get; } = [];

        // Lets the empty-Turns guard test assert that the seam was never touched at all, not merely
        // that no message was sent.
        public bool PrepareCalled { get; private set; }
        public bool CanaryCalled { get; private set; }

        // Fix round 1, Finding 1's orphan-survival test needs a delay that keeps running past the
        // runner's own timeout -- exactly like the real driver, whose underlying BotSharp call has
        // no cancellation hook at all. Default true preserves every pre-existing test's behavior
        // (SendDelay races against `ct`, so it stops as soon as the runner's timeout fires).
        public bool HonorCancellationInSend { get; set; } = true;

        // Fix round 1, Finding 3's test: simulates a cancellation that has nothing to do with the
        // runner's own timeout or the caller's ct (e.g. an HttpClient timeout inside a passthrough
        // tool call).
        public bool ThrowUnrelatedCancellation { get; set; }

        // Fix wave item 7's test: simulates the orphaned real driver call itself failing (not
        // just running long) after the runner has already given up waiting on it -- e.g. the
        // underlying BotSharp SendMessage call throws once it finally does return.
        public Exception? ThrowAfterDelay { get; set; }

        public Task PrepareAsync(string conversationId, string agentId, IReadOnlyList<TestState> initialStates)
        {
            PrepareCalled = true;
            PreparedAgentId = agentId;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Registry + tool names the seam should report as blocked on the first turn. The real
        /// blocking happens inside MockFunctionExecutor, which no fake driver goes through, so the
        /// observed call it would have recorded is reproduced here instead.
        /// </summary>
        public IAgentTestRunRegistry? Registry { get; set; }
        public List<string> BlockedOnFirstSend { get; } = [];

        public async Task<string?> SendAsync(string conversationId, string agentId, string userMessage, CancellationToken ct)
        {
            Sent.Add(userMessage);
            SentAgentIds.Add(agentId);
            OnSend?.Invoke();

            // Appended before any of the failure simulations below, like the real thing: BotSharp
            // has already written the assistant dialogs by the time a later step throws.
            string[] turnAgents = AgentsPerTurn.Count >= Sent.Count
                ? AgentsPerTurn[Sent.Count - 1]
                : RoutedAgent == null ? [] : [RoutedAgent];
            // Id mirrors the name here, because the runner collapses consecutive hops BY ID: giving
            // every agent the same blank id would collapse a genuine hand-off away.
            _agentSequence.AddRange(
                turnAgents.Select(name => new AgentChainHop { Id = name, Name = name }));

            if (BlockedOnFirstSend.Count > 0 && Sent.Count == 1)
            {
                var active = Registry?.TryGet(conversationId);
                foreach (var name in BlockedOnFirstSend)
                {
                    active?.Record(new ObservedToolCall
                    {
                        TurnIndex = active.CurrentTurnIndex,
                        FunctionName = name,
                        Outcome = "Blocked",
                        ResultContent = $"[agent-test] blocked unmocked tool: {name}"
                    });
                }
            }

            if (ThrowUnrelatedCancellation)
            {
                throw new OperationCanceledException(
                    "simulated unrelated cancellation, e.g. an internal deadline inside a passthrough tool call");
            }

            if (SendDelay > TimeSpan.Zero)
            {
                if (HonorCancellationInSend)
                {
                    await Task.Delay(SendDelay, ct);
                }
                else
                {
                    // Deliberately does NOT observe ct -- simulates BotSharp's real SendMessage,
                    // which keeps running after the runner gives up waiting on it.
                    await Task.Delay(SendDelay);
                }
            }

            if (ThrowAfterDelay != null)
            {
                throw ThrowAfterDelay;
            }

            return Replies.Count > 0 ? Replies.Dequeue() : string.Empty;
        }

        public Task<int> InjectHistoryAsync(
            string conversationId, string agentId, IReadOnlyList<TestHistoryMessage> history)
        {
            InjectedHistory.AddRange(history);

            // Mirrors the real driver: an authored assistant message becomes a stored assistant
            // dialog, so it turns up in the sequence and the runner has to exclude it from the chain.
            // A fake that skipped this would let a broken offset pass.
            foreach (var message in history)
            {
                if (string.Equals(message.Role, HistoryRoles.Assistant, StringComparison.OrdinalIgnoreCase))
                {
                    var name = HistoryAgentName ?? agentId;
                    _agentSequence.Add(new AgentChainHop { Id = name, Name = name });
                }
            }

            return Task.FromResult(HistoryWriteCount ?? history.Count);
        }

        public Task<bool> RunCanaryAsync(string conversationId, string agentId, CancellationToken ct)
        {
            CanaryCalled = true;
            CanaryAgentId = agentId;
            return Task.FromResult(CanaryResult);
        }

        public Task<IReadOnlyDictionary<string, string?>> ReadStatesAsync(string conversationId)
            => Task.FromResult<IReadOnlyDictionary<string, string?>>(States);

        public Task<IReadOnlyList<AgentChainHop>> ReadAssistantAgentSequenceAsync(string conversationId)
            => Task.FromResult<IReadOnlyList<AgentChainHop>>(_agentSequence.ToList());
    }

    /// <summary>
    /// Reports whatever it is told to. Only Total and AccumulatedCost are read by the runner -- the
    /// rest of ITokenStatistics exists for the completion providers that feed it.
    /// </summary>
    private sealed class FakeTokens : ITokenStatistics
    {
        public long Total { get; set; }
        public float AccumulatedCost { get; set; }
        public float Cost => AccumulatedCost;

        public Task AddToken(TokenStatsModel stats, RoleDialogModel message) => Task.CompletedTask;
        public void PrintStatistics() { }
        public void StartTimer() { }
        public void StopTimer() { }
    }

    private static AgentTestCaseRunner Build(
        FakeDriver driver, out AgentTestRunRegistry registry, ITokenStatistics? tokens = null)
    {
        registry = new AgentTestRunRegistry();
        return new AgentTestCaseRunner(
            registry, driver, NullLogger<AgentTestCaseRunner>.Instance, judge: null, tokens: tokens);
    }

    private static AgentTestSuite Suite(int timeoutSeconds = 120) => new()
    {
        Id = "suite-1",
        AgentId = "agent-1",
        Name = "s",
        CaseTimeoutSeconds = timeoutSeconds
    };

    [Fact]
    public async Task A_blocked_tool_fails_the_case_even_when_every_authored_assertion_passed()
    {
        // Blocking is the seam working: the agent reached for a tool this case does not mock, and
        // running it for real could have sent an email. But the block also stops that turn, so the
        // rest of the conversation never happened -- reporting Passed would be the same
        // "executed nothing, reports green" defect the no-turns and canary guards exist to prevent.
        var driver = new FakeDriver();
        driver.Replies.Enqueue("ok");
        var runner = Build(driver, out var registry);
        driver.Registry = registry;
        driver.BlockedOnFirstSend.Add("get_estimate_arrival_time");

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "no assertion covers the blocked tool",
            Turns = [new TestTurn { Index = 0, UserMessage = "when is the tech arriving" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Failed, result.Status);

        // Reported as an assertion so it lands in the ordinary results table, and as Failed rather
        // than Error because the harness did exactly its job.
        var blocked = Assert.Single(result.Assertions, a => a.Type == AssertionTypes.NoBlockedTools);
        Assert.False(blocked.Passed);
        Assert.Contains("get_estimate_arrival_time", blocked.Actual!);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task A_case_with_no_blocked_tools_gains_no_synthetic_assertion()
    {
        var driver = new FakeDriver();
        driver.Replies.Enqueue("ok");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "clean",
            Turns = [new TestTurn { Index = 0, UserMessage = "hello" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Passed, result.Status);
        Assert.DoesNotContain(result.Assertions, a => a.Type == AssertionTypes.NoBlockedTools);
    }

    [Fact]
    public async Task Drives_every_turn_in_order()
    {
        // Fix round 1, Finding 5: turns are enqueued OUT of index order (1 before 0) so that
        // removing the runner's `.OrderBy(t => t.Index)` sort would send "my sink leaks" first and
        // dequeue the replies in the wrong order, failing the assertions below. The original
        // fixture (already-ascending turns) let a missing sort go unnoticed.
        var driver = new FakeDriver();
        driver.Replies.Enqueue("first");
        driver.Replies.Enqueue("second");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "two turns",
            Turns =
            [
                new TestTurn { Index = 1, UserMessage = "my sink leaks" },
                new TestTurn { Index = 0, UserMessage = "hello" }
            ]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(["hello", "my sink leaks"], driver.Sent);
        Assert.Equal(AgentTestStatus.Passed, result.Status);
        Assert.Equal("first", result.Turns[0].Output);
        Assert.Equal("second", result.Turns[1].Output);
    }

    [Fact]
    public async Task Fails_the_case_without_running_the_agent_when_the_seam_is_not_live()
    {
        // The most dangerous silent failure in this whole feature: seam not live -> mocking does
        // nothing -> real tools get called.
        var driver = new FakeDriver { CanaryResult = false };
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "hello" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Contains("mock seam", result.Error!);
        Assert.Empty(driver.Sent);          // not a single message may go out
    }

    [Fact]
    public async Task A_case_with_no_turns_is_recorded_as_error_without_touching_the_driver()
    {
        // Turns.SelectMany(...).Concat(...).All(a => a.Passed) is vacuously true on an empty
        // sequence, and a case that ran no turns must never be reported as Passed. The check has to
        // come before the canary: if no turn will run, no conversation should be opened only to
        // report an error afterwards.
        var driver = new FakeDriver();
        var runner = Build(driver, out var registry);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "no turns",
            Turns = []
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Equal("the case has no turns", result.Error);
        Assert.False(driver.PrepareCalled);
        Assert.False(driver.CanaryCalled);
        Assert.Empty(driver.Sent);
        Assert.Null(registry.TryGet(result.ConversationId));
    }

    [Fact]
    public async Task A_failing_turn_assertion_fails_the_case_but_later_turns_still_run()
    {
        var driver = new FakeDriver();
        driver.Replies.Enqueue("nope");
        driver.Replies.Enqueue("second");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns =
            [
                new TestTurn
                {
                    Index = 0, UserMessage = "a",
                    Assertions = [new TestAssertion { Type = AssertionTypes.OutputContains, Expected = "yes" }]
                },
                new TestTurn { Index = 1, UserMessage = "b" }
            ]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Failed, result.Status);
        Assert.Equal(2, driver.Sent.Count);
    }

    [Fact]
    public async Task A_fatal_assertion_stops_the_remaining_turns()
    {
        var driver = new FakeDriver();
        driver.Replies.Enqueue("nope");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns =
            [
                new TestTurn
                {
                    Index = 0, UserMessage = "a",
                    Assertions = [new TestAssertion
                    {
                        Type = AssertionTypes.OutputContains, Expected = "yes", Fatal = true
                    }]
                },
                new TestTurn { Index = 1, UserMessage = "b" }
            ]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Failed, result.Status);
        Assert.Single(driver.Sent);
    }

    [Fact]
    public async Task Case_level_assertions_see_the_final_state_and_routed_agent()
    {
        var driver = new FakeDriver { RoutedAgent = "Work Order Creator" };
        driver.States["wo_id"] = "123";
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }],
            Assertions =
            [
                new TestAssertion { Type = AssertionTypes.StateEquals, Target = "wo_id", Expected = "123" },
                new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "work order creator" }
            ]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Passed, result.Status);
        Assert.All(result.Assertions, a => Assert.True(a.Passed));
    }

    [Fact]
    public async Task A_timeout_is_recorded_as_error_not_as_a_failed_assertion()
    {
        var driver = new FakeDriver { SendDelay = TimeSpan.FromSeconds(5) };
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(timeoutSeconds: 1), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Contains("timed out", result.Error!);
    }

    [Fact]
    public async Task An_unrelated_cancellation_is_recorded_with_its_real_message_not_mislabeled_as_a_timeout()
    {
        // Fix round 1, Finding 3. The old guard (`when (!ct.IsCancellationRequested)`) caught ANY
        // OperationCanceledException that wasn't the caller's own cancellation and stamped it "the
        // case timed out after Ns" -- even one that has nothing to do with the runner's own
        // per-case deadline, like an HttpClient timeout raised inside a passthrough tool call. This
        // pins that such a cancellation now falls through to the generic handler and keeps its own
        // message.
        var driver = new FakeDriver { ThrowUnrelatedCancellation = true };
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.DoesNotContain("timed out", result.Error!);
        Assert.Contains("simulated unrelated cancellation", result.Error!);
    }

    [Fact]
    public async Task The_conversation_is_unregistered_immediately_on_the_happy_path()
    {
        // Fix round 1, Finding 1 explicitly asks to keep a case proving the happy path still
        // unregisters synchronously -- none of the pre-existing tests actually asserted this
        // (the one that touched the registry at all did so only on the timeout path, and has been
        // replaced below by a pair of tests covering the new orphan-safe behavior).
        var driver = new FakeDriver();
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out var registry);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Passed, result.Status);
        Assert.Null(registry.TryGet(result.ConversationId));
    }

    [Fact]
    public async Task The_registry_entry_survives_an_orphaned_send_and_is_removed_once_it_actually_finishes()
    {
        // Fix round 1, Finding 1 (replaces the old The_conversation_is_always_unregistered_afterwards,
        // which only proved eventual cleanup -- a property the ORIGINAL, buggy code already
        // satisfied trivially by unregistering immediately). HonorCancellationInSend = false makes
        // the fake behave like the real driver: BotSharp's SendMessage has no cancellation hook, so
        // the runner's 1s timeout only stops the RUNNER from waiting; the "call" itself keeps
        // running for the full 2s. If the registry entry were removed the moment RunAsync returns
        // (the old behavior), TestMockExecutorProvider would stop intercepting for the ~1s the
        // orphan is still in flight -- exactly the window a real tool call could slip through in.
        var driver = new FakeDriver
        {
            // Raised from 2s to 5s (Task 8 pre-work): RunAsync returns at ~1s and the orphan
            // finished at ~2s, leaving only ~1s of slack before the Assert.NotNull below under
            // UnitTest's parallel-collection thread-pool contention (no xunit.runner.json here,
            // unlike the sequential AiPlatform-style suite). 5s widens that to ~4s of slack.
            // Fix round 1, finding 8: raising SendDelay alone only moved the tight margin rather
            // than removing it -- the trailing poll loop's deadline was left at .AddSeconds(5)
            // against an orphan that itself now takes ~5s, leaving only ~1s of slack there while
            // the first assert gained 3s. See the deadline below, now .AddSeconds(10), for both
            // asserts to have comparable (~4-5s) margin.
            SendDelay = TimeSpan.FromSeconds(5),
            HonorCancellationInSend = false
        };
        var runner = Build(driver, out var registry);

        var result = await runner.RunAsync(Suite(timeoutSeconds: 1), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Contains("timed out", result.Error!);

        // RunAsync returned at ~1s; the orphaned send does not finish until ~2s, so the entry MUST
        // still be there right now.
        Assert.NotNull(registry.TryGet(result.ConversationId));

        // ...and must be gone once that orphaned call has actually finished. Widened from 5s to
        // 10s alongside SendDelay's 2s->5s raise above (fix round 1, finding 8) -- the orphan now
        // finishes at ~5s (measured from this poll starting at ~1s), so a 5s deadline left only
        // ~1s of margin here, same tightness as before, just moved instead of removed.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (registry.TryGet(result.ConversationId) != null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
        Assert.Null(registry.TryGet(result.ConversationId));
    }

    [Fact]
    public async Task An_orphaned_call_that_later_faults_is_logged_instead_of_vanishing_silently()
    {
        // Fix wave item 7: the ContinueWith that unregisters an orphaned call never observed the
        // antecedent task's exception -- a real driver call that throws AFTER the case's own
        // timeout had already elapsed vanished with no trace beyond an UnobservedTaskException at
        // GC. This is the cheapest available diagnostic for the branch's single highest-risk
        // unverified property (a real call still running after RunAsync itself already returned).
        var driver = new FakeDriver
        {
            SendDelay = TimeSpan.FromSeconds(2),
            HonorCancellationInSend = false,
            ThrowAfterDelay = new InvalidOperationException(
                "simulated real-driver failure after the runner gave up waiting")
        };
        var logger = new CapturingLogger();
        var runner = new AgentTestCaseRunner(new AgentTestRunRegistry(), driver, logger);

        var result = await runner.RunAsync(Suite(timeoutSeconds: 1), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        // The case itself still reports its own timeout -- the orphan's eventual fault is
        // diagnostic-only and must never retroactively change what the case already reported.
        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Contains("timed out", result.Error!);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!logger.Entries.Any(e => e.Exception != null) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        var logged = Assert.Single(logger.Entries, e => e.Exception != null);
        Assert.Equal(LogLevel.Error, logged.Level);
        Assert.Contains("case-1", logged.Message);
        Assert.Contains("simulated real-driver failure", logged.Exception!.ToString());
    }

    [Fact]
    public async Task The_suite_allow_list_overrides_are_applied_to_the_active_run()
    {
        AgentTestCase testCase = new()
        {
            Id = "case-1", Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        };
        var suite = Suite();
        suite.ExtraAllowedFunctions.Add("util-db-sql_select");
        suite.ForceBlockedFunctions.Add("response_to_user");

        ActiveTestRun? captured = null;
        var driver = new FakeDriver();
        var registry = new AgentTestRunRegistry();
        var runner = new AgentTestCaseRunner(
            new CapturingRegistry(registry, r => captured = r),
            driver,
            NullLogger<AgentTestCaseRunner>.Instance);

        await runner.RunAsync(suite, testCase, "run-1", model: null, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Contains("util-db-sql_select", captured!.AllowedFunctions);
        Assert.Contains("route_to_agent", captured.AllowedFunctions);       // default allow list intact
        Assert.Contains("response_to_user", captured.ForceBlockedFunctions);
    }

    private sealed class CapturingRegistry(AgentTestRunRegistry inner, Action<ActiveTestRun> onRegister)
        : IAgentTestRunRegistry
    {
        public void Register(ActiveTestRun run) { onRegister(run); inner.Register(run); }
        public void Unregister(string conversationId) => inner.Unregister(conversationId);
        public ActiveTestRun? TryGet(string? conversationId) => inner.TryGet(conversationId);
    }

    /// <summary>
    /// Records every Log call so a test can assert an exception was actually observed and
    /// logged. ConcurrentBag, not List: the continuation that logs the orphan's fault runs on a
    /// ThreadPool thread (TaskScheduler.Default) racing the test's own polling read.
    /// </summary>
    private sealed class CapturingLogger : ILogger<AgentTestCaseRunner>
    {
        public ConcurrentBag<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception, formatter(state, exception)));
        }
    }

    [Fact]
    public async Task The_cases_entry_agent_overrides_the_suites_on_every_driver_call()
    {
        // This one value decides whether the router is part of what the case measures, because
        // BotSharp dispatches on the agent's own type (ConversationService.SendMessage: Routing ->
        // InstructLoop and can hand off, anything else -> InstructDirect and cannot). It has to win
        // on all three calls -- a case that prepares and sends against the entry agent but runs its
        // canary against the suite's would prove the seam live on the wrong conversation shape.
        var driver = new FakeDriver { RoutedAgent = "Work Order Creator" };
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out _);

        await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            EntryAgentId = "copilot-entry",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal("copilot-entry", driver.PreparedAgentId);
        Assert.Equal("copilot-entry", driver.CanaryAgentId);
        Assert.Equal(["copilot-entry"], driver.SentAgentIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_case_with_no_entry_agent_still_uses_the_suites(string? entryAgentId)
    {
        // Every case authored before EntryAgentId existed deserialises with it null, so the fallback
        // is what keeps those cases running against the same agent they always did. Blank strings go
        // the same way: a UI that sends "" for an untouched field must not silently retarget a case
        // at an agent id of "".
        var driver = new FakeDriver();
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out _);

        await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            EntryAgentId = entryAgentId,
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal("agent-1", driver.PreparedAgentId);
        Assert.Equal(["agent-1"], driver.SentAgentIds);
    }

    [Fact]
    public async Task The_agent_chain_collapses_consecutive_repeats_but_keeps_a_return_hop()
    {
        // A -> A is one agent sending two messages, not a hand-off, so it collapses. The second A is
        // a real return hop and must survive: flattening the chain to distinct agents would make an
        // ordered agentChain assertion unable to see the conversation come back.
        var driver = new FakeDriver();
        driver.AgentsPerTurn.Add(["Copilot", "Copilot", "Work Order Creator", "Copilot"]);
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(["Copilot", "Work Order Creator", "Copilot"], result.AgentChain);
    }

    [Fact]
    public async Task Each_turns_chain_is_only_that_turns_slice()
    {
        // The driver hands back the whole conversation every time, so a turn's chain is the runner's
        // own slice. The distinction is load-bearing here: turn 2's chain is ["Work Order Creator"]
        // even though the case-level chain collapses that agent together with turn 1's trailing
        // entry, so a per-turn chain cannot be derived from the case-level one.
        var driver = new FakeDriver();
        driver.AgentsPerTurn.Add(["Copilot", "Work Order Creator"]);
        driver.AgentsPerTurn.Add(["Work Order Creator"]);
        driver.Replies.Enqueue("one");
        driver.Replies.Enqueue("two");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns =
            [
                new TestTurn { Index = 0, UserMessage = "a" },
                new TestTurn { Index = 1, UserMessage = "b" }
            ]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(["Copilot", "Work Order Creator"], result.Turns[0].AgentChain);
        Assert.Equal(["Work Order Creator"], result.Turns[1].AgentChain);
        Assert.Equal(["Copilot", "Work Order Creator"], result.AgentChain);
    }

    [Fact]
    public async Task A_turn_that_produced_no_answer_routes_to_nobody()
    {
        // Turn-level routedToAgent reads that turn's own last agent, not the conversation's. Were it
        // the conversation's, this second turn would inherit turn 1's agent and pass an assertion
        // about a turn that never routed anywhere at all.
        var driver = new FakeDriver();
        driver.AgentsPerTurn.Add(["Work Order Creator"]);
        driver.AgentsPerTurn.Add([]);
        driver.Replies.Enqueue("one");
        driver.Replies.Enqueue("two");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns =
            [
                new TestTurn
                {
                    Index = 0,
                    UserMessage = "a",
                    Assertions =
                    [
                        new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "Work Order Creator" }
                    ]
                },
                new TestTurn
                {
                    Index = 1,
                    UserMessage = "b",
                    Assertions =
                    [
                        new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "Work Order Creator" }
                    ]
                }
            ]
        }, "run-1", model: null, CancellationToken.None);

        Assert.True(result.Turns[0].Assertions[0].Passed);
        Assert.False(result.Turns[1].Assertions[0].Passed);
        Assert.Empty(result.Turns[1].AgentChain);
    }

    [Fact]
    public async Task An_agent_chain_assertion_sees_a_hand_off_that_routed_to_agent_cannot()
    {
        // The reason agentChain exists. Control reaches the work order agent and comes back, so the
        // last agent to speak is the entry agent -- routedToAgent reports Copilot and a correctly
        // routed case looks like a routing failure. The chain still shows the hand-off happened.
        var driver = new FakeDriver();
        driver.AgentsPerTurn.Add(["Copilot", "Work Order Creator", "Copilot"]);
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }],
            Assertions =
            [
                new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "Work Order Creator" },
                new TestAssertion
                {
                    Type = AssertionTypes.AgentChain,
                    Target = AgentChainModes.Ordered,
                    Expected = "Copilot, Work Order Creator"
                }
            ]
        }, "run-1", model: null, CancellationToken.None);

        Assert.False(result.Assertions[0].Passed);
        Assert.Equal("Copilot", result.Assertions[0].Actual);
        Assert.True(result.Assertions[1].Passed);
    }

    [Fact]
    public async Task The_case_type_is_stamped_on_the_result_even_when_the_case_never_ran()
    {
        // Routing accuracy is aggregated from the result rows, so a row that cannot say what type of
        // case it came from would silently drop out of the figure. The no-turns guard returns before
        // the driver is touched at all, which is exactly the path most likely to forget the stamp.
        var driver = new FakeDriver();
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            CaseType = CaseTypes.Routing,
            Turns = []
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Equal(CaseTypes.Routing, result.CaseType);
        Assert.False(driver.PrepareCalled);
    }

    [Fact]
    public async Task Authored_history_is_written_before_the_first_turn_is_sent()
    {
        // The whole point is that the model sees the exchange as the conversation's opening context.
        // Written after the first turn it would be context for the second turn onwards, which is a
        // different scenario from the one the author wrote.
        var driver = new FakeDriver { RoutedAgent = "Work Order Creator" };
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            History =
            [
                new TestHistoryMessage { Role = HistoryRoles.User, Content = "my fridge is leaking" },
                new TestHistoryMessage { Role = HistoryRoles.Assistant, Content = "I raised work order B123." }
            ],
            Turns = [new TestTurn { Index = 0, UserMessage = "when is someone coming?" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Passed, result.Status);
        Assert.Equal(2, driver.InjectedHistory.Count);
        Assert.Equal("my fridge is leaking", driver.InjectedHistory[0].Content);
        Assert.Equal(["when is someone coming?"], driver.Sent);
    }

    [Fact]
    public async Task A_history_write_that_silently_did_nothing_errors_the_case()
    {
        // AppendConversationDialogs is an UpdateOne with no upsert, so a missing dialog document
        // makes it a no-op that reports success. Left unchecked, the case would run with no context
        // at all and report an ordinary pass or fail about a scenario that never existed -- the same
        // "executed nothing, reports green" family as the canary and the no-turns guard.
        var driver = new FakeDriver { HistoryWriteCount = 1 };
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            History =
            [
                new TestHistoryMessage { Role = HistoryRoles.User, Content = "a" },
                new TestHistoryMessage { Role = HistoryRoles.Assistant, Content = "b" }
            ],
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Contains("1 of 2 history messages", result.Error);
        // And it stopped there rather than running the case anyway.
        Assert.Empty(driver.Sent);
    }

    [Fact]
    public async Task Authored_history_is_excluded_from_the_agent_chain()
    {
        // Authored history is not something the agent under test did. Counting it would break the
        // one assertion that most needs to be trustworthy: an exact chain of a single agent is how a
        // case asserts nothing routed away, and a fabricated preamble in the chain would fail it for
        // a reason the author never caused.
        var driver = new FakeDriver { HistoryAgentName = "Copilot" };
        driver.AgentsPerTurn.Add(["Work Order Creator"]);
        driver.Replies.Enqueue("done");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            History = [new TestHistoryMessage { Role = HistoryRoles.Assistant, Content = "earlier answer" }],
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }],
            Assertions =
            [
                new TestAssertion
                {
                    Type = AssertionTypes.AgentChain,
                    Target = AgentChainModes.Exact,
                    Expected = "Work Order Creator"
                }
            ]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(["Work Order Creator"], result.AgentChain);
        Assert.Equal(["Work Order Creator"], result.Turns[0].AgentChain);
        Assert.True(result.Assertions[0].Passed);
    }

    [Fact]
    public async Task Token_usage_is_recorded_as_a_delta_not_an_absolute_reading()
    {
        // ITokenStatistics is scoped and the queue opens a scope per case, so on that path the
        // baseline is zero. But a scope that outlived an earlier case would otherwise have this case
        // billed for that one's tokens too, and a cost figure that charges the wrong case is worse
        // than none.
        var driver = new FakeDriver();
        driver.Replies.Enqueue("done");
        var tokens = new FakeTokens { Total = 500, AccumulatedCost = 0.05f };
        var runner = Build(driver, out _, tokens);

        // Simulates the conversation consuming 300 tokens during the case.
        driver.OnSend = () => { tokens.Total = 800; tokens.AccumulatedCost = 0.08f; };

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(300, result.TotalTokens);
        Assert.Equal(0.03, result.Cost, precision: 4);
    }

    [Fact]
    public async Task Usage_is_still_recorded_when_the_case_times_out()
    {
        // A run that fell over having burned the budget is exactly the run whose cost matters, so the
        // reading happens in the finally block rather than on the success path.
        var driver = new FakeDriver { SendDelay = TimeSpan.FromSeconds(5) };
        var tokens = new FakeTokens();
        var runner = Build(driver, out _, tokens);
        driver.OnSend = () => { tokens.Total = 120; tokens.AccumulatedCost = 0.01f; };

        var result = await runner.RunAsync(Suite(timeoutSeconds: 1), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Equal(120, result.TotalTokens);
    }

    [Fact]
    public async Task Model_duration_covers_the_agent_call_and_not_the_harness()
    {
        // A latency gate read against the case's whole wall clock also measures the canary, the mock
        // lookups and the conversation reads. On a fast case that overhead is a large enough share to
        // move a percentile, so the two figures are kept apart.
        var driver = new FakeDriver { SendDelay = TimeSpan.FromMilliseconds(120) };
        driver.Replies.Enqueue("one");
        driver.Replies.Enqueue("two");
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns =
            [
                new TestTurn { Index = 0, UserMessage = "a" },
                new TestTurn { Index = 1, UserMessage = "b" }
            ]
        }, "run-1", model: null, CancellationToken.None);

        // Two turns of ~120ms each, summed onto the case and recorded per turn.
        Assert.True(result.ModelDurationMs >= 200, $"was {result.ModelDurationMs}");
        Assert.True(result.Turns[0].ModelDurationMs >= 100, $"was {result.Turns[0].ModelDurationMs}");
        Assert.Equal(
            result.ModelDurationMs,
            result.Turns.Sum(t => t.ModelDurationMs));

        // And it is never larger than the case's own wall clock, which contains it.
        Assert.True(result.DurationMs >= result.ModelDurationMs);
    }

    [Fact]
    public async Task A_case_that_never_reached_the_model_reports_zero_model_time()
    {
        // What keeps a crashed run from looking like the fastest one on record: the executor drops
        // these from the latency percentile, and it can only do that because the figure is zero
        // rather than the wall clock of the failure.
        var driver = new FakeDriver { CanaryResult = false };
        var runner = Build(driver, out _);

        var result = await runner.RunAsync(Suite(), new AgentTestCase
        {
            Id = "case-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "a" }]
        }, "run-1", model: null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Equal(0, result.ModelDurationMs);
    }
}
