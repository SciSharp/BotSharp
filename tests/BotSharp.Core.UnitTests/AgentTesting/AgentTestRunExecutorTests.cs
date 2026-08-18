using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using BotSharp.Plugin.AgentTesting.Repositories;
using BotSharp.Plugin.AgentTesting.Services;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// Run-level orchestration: cases run serially, one failing case does not affect the rest, the counts
/// are right, and cancellation takes effect promptly. Serial execution is a safety choice rather than
/// a performance one -- cases share external dependencies, and running them concurrently would let
/// them pollute each other's state.
/// </summary>
public class AgentTestRunExecutorTests
{
    private sealed class InMemoryRepo : IAgentTestRepository
    {
        public AgentTestSuite Suite = new() { Id = "suite-1", AgentId = "agent-1", Name = "s" };
        public List<AgentTestCase> Cases = [];
        public AgentTestRun Run = new() { Id = "run-1", SuiteId = "suite-1" };
        public List<AgentTestCaseResult> Results = [];

        public Task<AgentTestSuite?> GetSuiteAsync(string id) => Task.FromResult<AgentTestSuite?>(Suite);
        public Task<List<AgentTestSuite>> ListSuitesAsync(string? agentId) => Task.FromResult(new List<AgentTestSuite> { Suite });
        public Task UpsertSuiteAsync(AgentTestSuite suite) => Task.CompletedTask;
        public Task DeleteSuiteAsync(string id) => Task.CompletedTask;
        public Task<AgentTestCase?> GetCaseAsync(string id) => Task.FromResult(Cases.FirstOrDefault(c => c.Id == id));
        public Task<List<AgentTestCase>> ListCasesAsync(string suiteId) => Task.FromResult(Cases);
        public Task UpsertCaseAsync(AgentTestCase testCase) => Task.CompletedTask;
        public Task DeleteCaseAsync(string id) => Task.CompletedTask;
        public Task<AgentTestRun> CreateRunAsync(AgentTestRun run) => Task.FromResult(run);
        public Task<AgentTestRun?> GetRunAsync(string id) => Task.FromResult<AgentTestRun?>(Run);
        public Task<List<AgentTestRun>> ListRunsAsync(string? suiteId) => Task.FromResult(new List<AgentTestRun> { Run });
        public Task<List<AgentTestRun>> ListRunsByStatusAsync(string status)
            => Task.FromResult(Run.Status == status ? new List<AgentTestRun> { Run } : []);
        public Task UpdateRunAsync(AgentTestRun run) { Run = run; return Task.CompletedTask; }
        public Task AddCaseResultAsync(AgentTestCaseResult result) { Results.Add(result); return Task.CompletedTask; }
        public Task<List<AgentTestCaseResult>> ListCaseResultsAsync(string runId) => Task.FromResult(Results);
    }

    /// <summary>
    /// Fix round 1. Unlike InMemoryRepo -- which always hands back the SAME AgentTestRun object
    /// reference, so "a stale local copy" and "a fresh read" are literally one object in every
    /// InMemoryRepo-based test -- this fake mimics what a real Mongo ReplaceOneAsync/Find round
    /// trip actually does: GetRunAsync always returns a brand-new CLONE of whatever was last
    /// written, so mutating a previously-returned object never affects what a LATER read sees;
    /// only UpdateRunAsync does. Only this fake can actually distinguish "the executor kept
    /// mutating/persisting a copy taken before a case ran" from "the executor picked up what
    /// changed while that case was executing."
    /// </summary>
    private sealed class CloningRunRepo : IAgentTestRepository
    {
        public AgentTestSuite Suite = new() { Id = "suite-1", AgentId = "agent-1", Name = "s" };
        public List<AgentTestCase> Cases = [];
        public List<AgentTestCaseResult> Results = [];

        private AgentTestRun _stored = new() { Id = "run-1", SuiteId = "suite-1" };

        /// <summary>The current backing state, for assertions -- a clone, so tests can't cheat by mutating it directly.</summary>
        public AgentTestRun Stored => Clone(_stored);

        /// <summary>Simulates a concurrent POST /runs/{id}/cancel landing directly in the backing store.</summary>
        public void SetCancelRequestedInBackingStore(bool value) => _stored.CancelRequested = value;

        public Task<AgentTestSuite?> GetSuiteAsync(string id) => Task.FromResult<AgentTestSuite?>(Suite);
        public Task<List<AgentTestSuite>> ListSuitesAsync(string? agentId) => Task.FromResult(new List<AgentTestSuite> { Suite });
        public Task UpsertSuiteAsync(AgentTestSuite suite) => Task.CompletedTask;
        public Task DeleteSuiteAsync(string id) => Task.CompletedTask;
        public Task<AgentTestCase?> GetCaseAsync(string id) => Task.FromResult(Cases.FirstOrDefault(c => c.Id == id));
        public Task<List<AgentTestCase>> ListCasesAsync(string suiteId) => Task.FromResult(Cases);
        public Task UpsertCaseAsync(AgentTestCase testCase) => Task.CompletedTask;
        public Task DeleteCaseAsync(string id) => Task.CompletedTask;
        public Task<AgentTestRun> CreateRunAsync(AgentTestRun run) => Task.FromResult(run);
        public Task<AgentTestRun?> GetRunAsync(string id) => Task.FromResult<AgentTestRun?>(Clone(_stored));
        public Task<List<AgentTestRun>> ListRunsAsync(string? suiteId) => Task.FromResult(new List<AgentTestRun> { Clone(_stored) });
        public Task<List<AgentTestRun>> ListRunsByStatusAsync(string status)
            => Task.FromResult(_stored.Status == status ? new List<AgentTestRun> { Clone(_stored) } : []);
        public Task UpdateRunAsync(AgentTestRun run) { _stored = Clone(run); return Task.CompletedTask; }
        public Task AddCaseResultAsync(AgentTestCaseResult result) { Results.Add(result); return Task.CompletedTask; }
        public Task<List<AgentTestCaseResult>> ListCaseResultsAsync(string runId) => Task.FromResult(Results);

        private static AgentTestRun Clone(AgentTestRun source) => new()
        {
            Id = source.Id,
            SuiteId = source.SuiteId,
            Status = source.Status,
            TriggeredBy = source.TriggeredBy,
            CaseIds = source.CaseIds,
            TotalCount = source.TotalCount,
            PassedCount = source.PassedCount,
            FailedCount = source.FailedCount,
            ErrorCount = source.ErrorCount,
            CancelRequested = source.CancelRequested,
            StartedAt = source.StartedAt,
            CompletedAt = source.CompletedAt,
            CreateDate = source.CreateDate
        };
    }

    private static AgentTestCase CaseNamed(string id) => new()
    {
        Id = id, SuiteId = "suite-1", Name = id,
        Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
    };

    private static AgentTestRunExecutor Build(InMemoryRepo repo, Func<AgentTestCase, AgentTestCaseResult> run)
        => BuildWithRunner(repo, run).Executor;

    private static (AgentTestRunExecutor Executor, DelegatingCaseRunner Runner) BuildWithRunner(
        InMemoryRepo repo, Func<AgentTestCase, AgentTestCaseResult> run)
    {
        var runner = new DelegatingCaseRunner(run);
        return (new AgentTestRunExecutor(repo, runner, NullLogger<AgentTestRunExecutor>.Instance), runner);
    }

    private sealed class DelegatingCaseRunner(Func<AgentTestCase, AgentTestCaseResult> run) : ICaseRunner
    {
        /// <summary>Every (case, model) pair the executor asked for, in the order it asked.</summary>
        public List<(string CaseId, string? Model)> Invocations { get; } = [];

        public Task<AgentTestCaseResult> RunAsync(
            AgentTestSuite suite, AgentTestCase testCase, string runId, TestModel? model, CancellationToken ct)
        {
            Invocations.Add((testCase.Id, model?.Model));
            var result = run(testCase);
            // The real runner stamps these onto the result; mirror it so tests can assert that a
            // result can be attributed back to the model that produced it.
            result.Provider ??= model?.Provider;
            result.Model ??= model?.Model;
            return Task.FromResult(result);
        }
    }

    [Fact]
    public async Task Counts_passed_failed_and_errored_cases()
    {
        // Fix round 1, finding 4: the brief's original 1-passed/1-failed/1-error mix can't tell a
        // Passed/Failed arm-swap bug apart from correct code (both produce 1/1/1 either way).
        // Two passed cases makes that class of bug visible (a swap would report 1 passed, not 2).
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), CaseNamed("b"), CaseNamed("c"), CaseNamed("d")] };
        var executor = Build(repo, c => new AgentTestCaseResult
        {
            CaseId = c.Id,
            Status = c.Id switch
            {
                "a" => AgentTestStatus.Passed,
                "d" => AgentTestStatus.Passed,
                "b" => AgentTestStatus.Failed,
                _ => AgentTestStatus.Error
            }
        });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Equal(4, repo.Run.TotalCount);
        Assert.Equal(2, repo.Run.PassedCount);
        Assert.Equal(1, repo.Run.FailedCount);
        Assert.Equal(1, repo.Run.ErrorCount);
        Assert.NotNull(repo.Run.CompletedAt);
    }

    [Fact]
    public async Task Every_enabled_case_runs_once_per_requested_model()
    {
        // The whole point of the model dimension: one run has to produce a full case x model grid,
        // otherwise "compare gpt-4o against claude on the same suite" needs two runs and a human
        // diffing two pages.
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), CaseNamed("b")] };
        repo.Run.Models =
        [
            new TestModel { Provider = "openai", Model = "gpt-4o" },
            new TestModel { Provider = "anthropic", Model = "claude-3-7-sonnet-20250219" }
        ];
        var (executor, runner) = BuildWithRunner(repo, c => new AgentTestCaseResult
        {
            CaseId = c.Id,
            Status = AgentTestStatus.Passed
        });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        // Case-major order, so the first case's comparison is complete before the second starts.
        Assert.Equal(
            [("a", "gpt-4o"), ("a", "claude-3-7-sonnet-20250219"), ("b", "gpt-4o"), ("b", "claude-3-7-sonnet-20250219")],
            runner.Invocations);

        // TotalCount is cases x models, not cases -- a run of 2 cases over 2 models is 4 executions
        // and every one of them costs real tokens.
        Assert.Equal(4, repo.Run.TotalCount);
        Assert.Equal(4, repo.Run.PassedCount);
        Assert.Equal(4, repo.Results.Count);

        // Attribution: without provider/model on the result the grid cannot be built at all.
        Assert.Equal(2, repo.Results.Count(r => r.Model == "gpt-4o"));
        Assert.All(repo.Results.Where(r => r.Model == "gpt-4o"), r => Assert.Equal("openai", r.Provider));
    }

    [Fact]
    public async Task No_requested_model_still_runs_each_case_exactly_once()
    {
        // Back-compat: every run document written before the model dimension existed has no Models
        // field, and every caller that omits it must keep the old one-pass behaviour.
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), CaseNamed("b")] };
        repo.Run.Models = null;
        var (executor, runner) = BuildWithRunner(repo, c => new AgentTestCaseResult
        {
            CaseId = c.Id,
            Status = AgentTestStatus.Passed
        });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Equal([("a", null), ("b", null)], runner.Invocations);
        Assert.Equal(2, repo.Run.TotalCount);
        Assert.All(repo.Results, r => Assert.Null(r.Model));
    }

    [Fact]
    public async Task One_crashing_case_does_not_abort_the_rest_of_the_run()
    {
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), CaseNamed("b")] };
        var executor = Build(repo, c => c.Id == "a"
            ? throw new InvalidOperationException("boom")
            : new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Equal(2, repo.Results.Count);
        Assert.Equal(AgentTestStatus.Error, repo.Results[0].Status);
        Assert.Equal(AgentTestStatus.Passed, repo.Results[1].Status);
    }

    [Fact]
    public async Task A_disabled_suite_ends_the_run_as_error_without_running_any_case()
    {
        // Fix wave item 8a: POST .../suites/{id}/run is the primary place a disabled suite is
        // rejected (400, before a run row exists at all -- see AgentTestControllerTests). This
        // pins the executor's own defense-in-depth for the race where a suite is disabled AFTER a
        // run was already queued: same "infrastructure stop" shape as the pre-existing
        // suite-no-longer-exists handling right above this check in the production code.
        var repo = new InMemoryRepo { Cases = [CaseNamed("a")] };
        repo.Suite.Enabled = false;
        var executor = Build(repo, c => new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Empty(repo.Results);
        Assert.Equal(AgentTestStatus.Error, repo.Run.Status);
        Assert.NotNull(repo.Run.CompletedAt);
    }

    [Fact]
    public async Task Skips_disabled_cases()
    {
        var disabled = CaseNamed("b");
        disabled.Enabled = false;
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), disabled] };
        var executor = Build(repo, c => new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Single(repo.Results);
        // Fix round 1, finding 4: Assert.Single alone can't tell "kept the enabled case" apart
        // from "kept the disabled one by an inverted filter" -- both leave exactly one result.
        Assert.Equal("a", repo.Results[0].CaseId);
    }

    [Fact]
    public async Task Stops_between_cases_once_cancellation_is_requested()
    {
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), CaseNamed("b"), CaseNamed("c")] };
        var executor = Build(repo, c =>
        {
            repo.Run.CancelRequested = true;      // cancel requested once the first case finished
            return new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed };
        });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Single(repo.Results);
        Assert.Equal(AgentTestStatus.Cancelled, repo.Run.Status);
    }

    [Fact]
    public async Task Runs_only_the_cases_named_by_CaseIds_when_specified()
    {
        // Important 2: re-running only a caller-selected subset (e.g. "just the cases that failed
        // last time") is core value for a regression harness, not a nice-to-have.
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), CaseNamed("b"), CaseNamed("c")] };
        repo.Run.CaseIds = ["b"];
        var executor = Build(repo, c => new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Single(repo.Results);
        Assert.Equal("b", repo.Results[0].CaseId);
        Assert.Equal(1, repo.Run.TotalCount);
    }

    [Fact]
    public async Task A_CaseIds_list_that_names_no_runnable_case_ends_the_run_as_Error_not_Passed()
    {
        // Task 9 fix: same "nothing executed, reports green" defect class as
        // AgentTestCaseRunner's "the case has no turns" guard -- FailedCount == 0 &&
        // ErrorCount == 0 a few lines below is vacuously true when the loop never runs at all,
        // which is exactly what happens when every id named by a non-empty CaseIds filter is
        // unknown (or names a disabled case) in this suite. Before this fix the run persisted
        // Status = Passed with TotalCount = 0 and zero result rows.
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), CaseNamed("b")] };
        repo.Run.CaseIds = ["does-not-exist"];
        var executor = Build(repo, c => new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Empty(repo.Results);
        Assert.Equal(AgentTestStatus.Error, repo.Run.Status);
        Assert.Equal(0, repo.Run.TotalCount);
        Assert.NotNull(repo.Run.CompletedAt);
    }

    [Fact]
    public async Task An_empty_CaseIds_list_still_runs_every_enabled_case()
    {
        // CaseIds is meant to narrow the run; an empty (as opposed to null) list must not be
        // read as "run nothing" -- that would make triggering a run with a request body like
        // `{}` (which model-binds CaseIds to null, not []) behave differently from one that
        // explicitly sends `{"caseIds": []}`, which is not the contract.
        var repo = new InMemoryRepo { Cases = [CaseNamed("a"), CaseNamed("b")] };
        repo.Run.CaseIds = [];
        var executor = Build(repo, c => new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed });

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Equal(2, repo.Results.Count);
    }

    [Fact]
    public async Task Persists_the_running_totals_after_each_case_not_only_at_the_end()
    {
        // Important 1: GET /agent-test/runs/{id} is the only progress surface while a run is in
        // flight. If counters are only written once at the very end, a 10-case suite at the
        // default 120s-per-case timeout would show 0/0/0/0 for up to 20 minutes, and a process
        // death mid-run would leave the startup sweep's Error row claiming zero cases ran even
        // though N AgentTestCaseResult rows already exist.
        //
        // This has to use CloningRunRepo, not InMemoryRepo: InMemoryRepo's GetRunAsync/Run field
        // is one shared object, so `run.TotalCount++` inside the executor and `repo.Run.TotalCount`
        // read from a test are the SAME mutation observed instantly either way -- that would make
        // this test pass identically whether or not the executor ever actually calls
        // UpdateRunAsync per case (confirmed live: this exact test, written against InMemoryRepo,
        // passed against the round-1 code that only persists once at the very end). CloningRunRepo
        // only updates its backing state -- what repo.Stored reads -- when UpdateRunAsync is
        // actually called, so it is the only fake that can tell "mutated a local object" apart from
        // "persisted."
        var repo = new CloningRunRepo { Cases = [CaseNamed("a"), CaseNamed("b")] };
        var observedTotalBeforeSecondCase = -1;
        var executor = new AgentTestRunExecutor(
            repo,
            new DelegatingCaseRunner(c =>
            {
                if (c.Id == "b")
                {
                    observedTotalBeforeSecondCase = repo.Stored.TotalCount;
                }
                return new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed };
            }),
            NullLogger<AgentTestRunExecutor>.Instance);

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        Assert.Equal(1, observedTotalBeforeSecondCase);
        Assert.Equal(2, repo.Stored.TotalCount);
    }

    [Fact]
    public async Task A_cancel_that_arrives_while_the_only_case_is_executing_still_survives_the_terminal_write()
    {
        // Fix round 1, "Minor 3": a cancel landing directly in the backing store WHILE the last
        // (here, only) case is running has no further loop iteration left to notice it via the
        // normal CancelRequested check. Proving the fix needs a repo that returns a genuinely
        // DIFFERENT object per read (CloningRunRepo) -- InMemoryRepo's shared-reference behavior
        // means "stale copy" and "fresh read" are the same object, so it can't distinguish a
        // correct terminal persist from one that clobbers the flag with a stale in-memory copy.
        var repo = new CloningRunRepo { Cases = [CaseNamed("a")] };
        var executor = new AgentTestRunExecutor(
            repo,
            new DelegatingCaseRunner(c =>
            {
                // Simulate the concurrent POST /runs/{id}/cancel landing in the backing store
                // while this case is "executing" -- after this case's own CancelRequested check
                // already read false, with no case after it to re-check before the run ends.
                repo.SetCancelRequestedInBackingStore(true);
                return new AgentTestCaseResult { CaseId = c.Id, Status = AgentTestStatus.Passed };
            }),
            NullLogger<AgentTestRunExecutor>.Instance);

        await executor.ExecuteAsync("run-1", CancellationToken.None);

        // The run legitimately finished the only case it was ever going to run; Status reflects
        // that real outcome, not a retroactive cancellation it never actually detected in time to
        // act on.
        Assert.Equal(AgentTestStatus.Passed, repo.Stored.Status);
        // But the flag an external caller set mid-flight must survive the terminal whole-document
        // replace, not get silently overwritten back to false by a copy read before that write
        // happened.
        Assert.True(repo.Stored.CancelRequested);
    }
}
