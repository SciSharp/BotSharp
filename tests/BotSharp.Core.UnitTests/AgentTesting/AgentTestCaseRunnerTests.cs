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

        public Task<bool> RunCanaryAsync(string conversationId, string agentId, CancellationToken ct)
        {
            CanaryCalled = true;
            return Task.FromResult(CanaryResult);
        }

        public Task<IReadOnlyDictionary<string, string?>> ReadStatesAsync(string conversationId)
            => Task.FromResult<IReadOnlyDictionary<string, string?>>(States);

        public Task<string?> ReadRoutedAgentNameAsync(string conversationId)
            => Task.FromResult(RoutedAgent);
    }

    private static AgentTestCaseRunner Build(FakeDriver driver, out AgentTestRunRegistry registry)
    {
        registry = new AgentTestRunRegistry();
        return new AgentTestCaseRunner(registry, driver, NullLogger<AgentTestCaseRunner>.Instance);
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
}
