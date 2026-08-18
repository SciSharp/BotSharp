using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Infrastructures.Attributes;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using BotSharp.Plugin.AgentTesting.Controllers;
using BotSharp.Plugin.AgentTesting.Models;
using BotSharp.Plugin.AgentTesting.Repositories;
using BotSharp.Plugin.AgentTesting.Services;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// Fix-wave coverage for the controller-level guards added on top of the ten already-shipped
/// tasks: rejecting the never-implemented Passthrough policy and a malformed assertion at case
/// create/update (before either can ever run and vacuously pass), rejecting a trigger against a
/// disabled suite, keeping UpdateSuite from blanking agentId/name on a partial body, rejecting a
/// cancel against an already-terminal run, and the [BotSharpAuth] gate on the two endpoints that
/// are this feature's PII/cost surfaces (RecordCase/TriggerRun).
/// </summary>
public class AgentTestControllerTests
{
    private sealed class InMemoryRepo : IAgentTestRepository
    {
        public Dictionary<string, AgentTestSuite> Suites { get; } = [];
        public Dictionary<string, AgentTestCase> Cases { get; } = [];
        public Dictionary<string, AgentTestRun> Runs { get; } = [];

        public Task<AgentTestSuite?> GetSuiteAsync(string id)
            => Task.FromResult(Suites.TryGetValue(id, out var s) ? s : null);
        public Task<List<AgentTestSuite>> ListSuitesAsync(string? agentId) => Task.FromResult(Suites.Values.ToList());
        public Task UpsertSuiteAsync(AgentTestSuite suite)
        {
            if (string.IsNullOrEmpty(suite.Id))
            {
                suite.Id = Guid.NewGuid().ToString();
            }
            Suites[suite.Id] = suite;
            return Task.CompletedTask;
        }
        public Task DeleteSuiteAsync(string id) { Suites.Remove(id); return Task.CompletedTask; }

        public Task<AgentTestCase?> GetCaseAsync(string id)
            => Task.FromResult(Cases.TryGetValue(id, out var c) ? c : null);
        public Task<List<AgentTestCase>> ListCasesAsync(string suiteId)
            => Task.FromResult(Cases.Values.Where(c => c.SuiteId == suiteId).ToList());
        public Task UpsertCaseAsync(AgentTestCase testCase)
        {
            // Mirrors the real AgentTestRepository: ReplaceOneAsync(upsert:true) does not run the
            // [BsonId(IdGenerator=...)] hook, so a brand-new case with no Id must get one here too,
            // or this fake diverges from production behaviour on exactly the create path these
            // tests exercise.
            if (string.IsNullOrEmpty(testCase.Id))
            {
                testCase.Id = Guid.NewGuid().ToString();
            }
            Cases[testCase.Id] = testCase;
            return Task.CompletedTask;
        }
        public Task DeleteCaseAsync(string id) { Cases.Remove(id); return Task.CompletedTask; }

        public Task<AgentTestRun> CreateRunAsync(AgentTestRun run)
        {
            if (string.IsNullOrEmpty(run.Id))
            {
                run.Id = Guid.NewGuid().ToString();
            }
            Runs[run.Id] = run;
            return Task.FromResult(run);
        }
        public Task<AgentTestRun?> GetRunAsync(string id) => Task.FromResult(Runs.TryGetValue(id, out var r) ? r : null);
        public Task<List<AgentTestRun>> ListRunsAsync(string? suiteId) => Task.FromResult(Runs.Values.ToList());
        public Task<List<AgentTestRun>> ListRunsByStatusAsync(string status)
            => Task.FromResult(Runs.Values.Where(r => r.Status == status).ToList());
        public Task UpdateRunAsync(AgentTestRun run) { Runs[run.Id] = run; return Task.CompletedTask; }

        public Task AddCaseResultAsync(AgentTestCaseResult result) => Task.CompletedTask;
        public Task<List<AgentTestCaseResult>> ListCaseResultsAsync(string runId) => Task.FromResult(new List<AgentTestCaseResult>());
    }

    private sealed class RecordingQueue : IAgentTestRunQueue
    {
        public List<string> Enqueued { get; } = [];
        public void Enqueue(string runId) => Enqueued.Add(runId);
    }

    /// <summary>
    /// A provider service that recognises exactly the models named here. The default (nothing
    /// registered) leaves every existing test unaffected -- validation short-circuits on an empty
    /// model list -- while forcing any test that DOES request a model to say so explicitly rather
    /// than passing against a permissive mock.
    /// </summary>
    private static ILlmProviderService ProviderServiceKnowing(params string[] providerSlashModel)
    {
        var known = new HashSet<string>(providerSlashModel, StringComparer.OrdinalIgnoreCase);
        var mock = new Mock<ILlmProviderService>();
        mock.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string p, string m) => known.Contains($"{p}/{m}") ? new LlmModelSetting { Name = m } : null);
        return mock.Object;
    }

    private static AgentTestController BuildController(
        InMemoryRepo repo, out RecordingQueue queue, ILlmProviderService? llmProviders = null)
    {
        var recorder = new AgentTestRecorder(
            Mock.Of<IBotSharpRepository>(),
            repo,
            NullLogger<AgentTestRecorder>.Instance);
        queue = new RecordingQueue();

        var controller = new AgentTestController(
            repo, queue, Mock.Of<IAgentService>(), recorder, llmProviders ?? ProviderServiceKnowing());

        // TriggerRun reads User.FindFirstValue(ClaimTypes.NameIdentifier) -- a directly-constructed
        // controller (no MVC pipeline/TestServer) has no HttpContext at all by default, and
        // ControllerBase.User dereferences it, so this must be wired up even for an anonymous test
        // caller (an identity with no NameIdentifier claim is fine; FindFirstValue just returns null
        // for that, same as a real anonymous-but-authenticated request would).
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        return controller;
    }

    private static AgentTestCaseUpsertRequest ValidRequest(string suiteId = "suite-1") => new()
    {
        SuiteId = suiteId,
        Name = "case",
        Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
    };

    private static AgentTestSuite EnabledSuite(string id = "suite-1") => new()
    {
        Id = id, AgentId = "agent-1", Name = "s", Enabled = true
    };

    // ---- Item 1: Passthrough is rejected, Block still works --------------------------------

    [Fact]
    public async Task Create_rejects_the_passthrough_policy()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out _);

        var request = ValidRequest();
        request.UnmockedToolPolicy = "Passthrough";

        var result = await controller.CreateCase(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(repo.Cases);
    }

    [Fact]
    public async Task Update_rejects_the_passthrough_policy_and_leaves_the_stored_case_untouched()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        repo.Cases["case-1"] = new AgentTestCase
        {
            Id = "case-1", SuiteId = "suite-1", Name = "original",
            UnmockedToolPolicy = UnmockedToolPolicies.Block
        };
        var controller = BuildController(repo, out _);

        var request = ValidRequest();
        request.UnmockedToolPolicy = "Passthrough";

        var result = await controller.UpdateCase("case-1", request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("original", repo.Cases["case-1"].Name);
        Assert.Equal(UnmockedToolPolicies.Block, repo.Cases["case-1"].UnmockedToolPolicy);
    }

    [Fact]
    public async Task Passthrough_rejection_is_case_insensitive()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out _);

        var request = ValidRequest();
        request.UnmockedToolPolicy = "passthrough";

        var result = await controller.CreateCase(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_accepts_the_default_block_policy()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out _);

        var result = await controller.CreateCase(ValidRequest());

        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        Assert.Single(repo.Cases);
    }

    // ---- Item 4: an assertion missing its required field is rejected at save time -----------

    [Theory]
    [InlineData(AssertionTypes.OutputContains)]
    [InlineData(AssertionTypes.OutputNotContains)]
    [InlineData(AssertionTypes.OutputRegex)]
    [InlineData(AssertionTypes.RoutedToAgent)]
    [InlineData(AssertionTypes.LlmJudge)]
    public async Task Create_rejects_a_case_level_assertion_missing_its_required_expected_value(string type)
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out _);

        var request = ValidRequest();
        request.Assertions = [new TestAssertion { Type = type }];

        var result = await controller.CreateCase(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(repo.Cases);
    }

    [Theory]
    [InlineData(AssertionTypes.ToolCalled)]
    [InlineData(AssertionTypes.ToolNotCalled)]
    [InlineData(AssertionTypes.StateEquals)]
    public async Task Create_rejects_a_turn_level_assertion_missing_its_required_target(string type)
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out _);

        var request = ValidRequest();
        request.Turns[0].Assertions = [new TestAssertion { Type = type }];

        var result = await controller.CreateCase(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(repo.Cases);
    }

    [Fact]
    public async Task Update_rejects_a_case_missing_a_required_assertion_field_too()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        repo.Cases["case-1"] = new AgentTestCase { Id = "case-1", SuiteId = "suite-1", Name = "original" };
        var controller = BuildController(repo, out _);

        var request = ValidRequest();
        request.Assertions = [new TestAssertion { Type = AssertionTypes.StateEquals, Target = "" }];

        var result = await controller.UpdateCase("case-1", request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("original", repo.Cases["case-1"].Name);
    }

    [Fact]
    public async Task Create_accepts_well_formed_assertions_of_every_type()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out _);

        var request = ValidRequest();
        request.Assertions =
        [
            new TestAssertion { Type = AssertionTypes.OutputContains, Expected = "ok" },
            new TestAssertion { Type = AssertionTypes.OutputNotContains, Expected = "sorry" },
            new TestAssertion { Type = AssertionTypes.OutputRegex, Expected = "^ok$" },
            new TestAssertion { Type = AssertionTypes.ToolCalled, Target = "get_work_order" },
            new TestAssertion { Type = AssertionTypes.ToolNotCalled, Target = "send_text_message" },
            new TestAssertion { Type = AssertionTypes.StateEquals, Target = "wo_id", Expected = "1" },
            new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "Router" },
            new TestAssertion { Type = AssertionTypes.LlmJudge, Expected = "criteria", MinScore = 0.8 }
        ];

        var result = await controller.CreateCase(request);

        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
    }

    // ---- Item 8a: a disabled suite cannot be triggered --------------------------------------

    [Fact]
    public async Task TriggerRun_rejects_a_disabled_suite_and_never_enqueues_it()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = new AgentTestSuite { Id = "suite-1", AgentId = "agent-1", Name = "s", Enabled = false };
        var controller = BuildController(repo, out var queue);

        var result = await controller.TriggerRun("suite-1", null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(queue.Enqueued);
        Assert.Empty(repo.Runs);
    }

    [Fact]
    public async Task TriggerRun_enqueues_an_enabled_suite()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out var queue);

        var result = await controller.TriggerRun("suite-1", null);

        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        Assert.Single(queue.Enqueued);
    }

    [Fact]
    public async Task TriggerRun_persists_the_requested_models_on_the_run()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(
            repo, out _, ProviderServiceKnowing("openai/gpt-4o", "anthropic/claude-3-7-sonnet-20250219"));

        var result = await controller.TriggerRun("suite-1", new AgentTestRunTriggerRequest
        {
            Models =
            [
                new TestModel { Provider = "openai", Model = "gpt-4o" },
                new TestModel { Provider = "anthropic", Model = "claude-3-7-sonnet-20250219" }
            ]
        });

        Assert.Null(result.Result);
        Assert.Equal(2, result.Value!.Models!.Count);
    }

    [Fact]
    public async Task TriggerRun_normalises_an_empty_model_list_to_null()
    {
        // Empty and absent both mean "the agent's own LlmConfig"; persisting [] would read like a
        // deliberate (and impossible) choice of zero models.
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out _);

        var result = await controller.TriggerRun("suite-1", new AgentTestRunTriggerRequest { Models = [] });

        Assert.Null(result.Value!.Models);
    }

    [Fact]
    public async Task TriggerRun_rejects_a_model_this_host_cannot_run()
    {
        // Measured before this guard existed: an unregistered model name reached the provider and
        // every case in the run died with a bare "Object reference not set to an instance of an
        // object." -- no run should be queued, and no tokens spent, for a typo.
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out var queue, ProviderServiceKnowing("openai/gpt-4o"));

        var result = await controller.TriggerRun("suite-1", new AgentTestRunTriggerRequest
        {
            Models = [new TestModel { Provider = "openai", Model = "definitely-not-a-real-model" }]
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(queue.Enqueued);
        Assert.Empty(repo.Runs);
    }

    [Fact]
    public async Task TriggerRun_rejects_the_same_model_listed_twice()
    {
        // Two identical columns would collapse into one cell in the comparison grid, the second
        // result silently overwriting the first.
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out var queue, ProviderServiceKnowing("openai/gpt-4o"));

        var result = await controller.TriggerRun("suite-1", new AgentTestRunTriggerRequest
        {
            Models =
            [
                new TestModel { Provider = "openai", Model = "gpt-4o" },
                new TestModel { Provider = "openai", Model = "gpt-4o" }
            ]
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(queue.Enqueued);
    }

    // ---- Item 3: the PII/cost surfaces require BotSharp's admin/root gate -------------------

    [Fact]
    public void RecordCase_and_TriggerRun_carry_the_BotSharpAuth_admin_gate()
    {
        // RecordCase copies a real conversation's raw content (possibly PII: phone numbers,
        // addresses, tenant names) into the test store; TriggerRun spends real model tokens with
        // no quota. Both must be gated tighter than the controller's own class-level [Authorize],
        // using the SAME mechanism BotSharp's own UserController/AgentController/PluginController/
        // RoleController already use for their sensitive actions -- BotSharpAuthAttribute checks
        // the caller is an admin/root user (see IsAdminUser).
        var recordMethod = typeof(AgentTestController).GetMethod(nameof(AgentTestController.RecordCase))!;
        var triggerMethod = typeof(AgentTestController).GetMethod(nameof(AgentTestController.TriggerRun))!;

        Assert.NotNull(recordMethod.GetCustomAttribute<BotSharpAuthAttribute>());
        Assert.NotNull(triggerMethod.GetCustomAttribute<BotSharpAuthAttribute>());
    }

    [Fact]
    public void Plain_read_and_CRUD_endpoints_do_not_carry_the_admin_only_gate()
    {
        // The gate is deliberately narrow: everything else (list/get/create/update/delete suites
        // and cases, list runs, mock-targets) stays under the controller's plain [Authorize] --
        // over-gating would block ordinary QA/PM test authoring, not just the PII/cost surfaces.
        var otherMethods = new[]
        {
            nameof(AgentTestController.ListSuites),
            nameof(AgentTestController.CreateSuite),
            nameof(AgentTestController.UpdateSuite),
            nameof(AgentTestController.DeleteSuite),
            nameof(AgentTestController.ListCases),
            nameof(AgentTestController.CreateCase),
            nameof(AgentTestController.UpdateCase),
            nameof(AgentTestController.DeleteCase),
            nameof(AgentTestController.ListRuns),
            nameof(AgentTestController.GetRun),
            nameof(AgentTestController.CancelRun),
            nameof(AgentTestController.GetMockTargets),
        };

        foreach (var name in otherMethods)
        {
            var method = typeof(AgentTestController).GetMethod(name)!;
            Assert.Null(method.GetCustomAttribute<BotSharpAuthAttribute>());
        }
    }

    // ---- Item 8b: UpdateSuite no longer blanks agentId/name on a partial body ---------------

    [Fact]
    public async Task UpdateSuite_keeps_the_existing_agentId_and_name_when_the_request_omits_them()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = new AgentTestSuite
        {
            Id = "suite-1", AgentId = "agent-1", Name = "original name", Enabled = true
        };
        var controller = BuildController(repo, out _);

        // A partial body: AgentId/Name are left at their DTO defaults (empty string), as a
        // caller that only means to flip, say, CaseTimeoutSeconds would send.
        var request = new AgentTestSuiteUpsertRequest { CaseTimeoutSeconds = 30 };

        var result = await controller.UpdateSuite("suite-1", request);

        Assert.Null(result.Result);
        Assert.Equal("agent-1", repo.Suites["suite-1"].AgentId);
        Assert.Equal("original name", repo.Suites["suite-1"].Name);
        Assert.Equal(30, repo.Suites["suite-1"].CaseTimeoutSeconds);
    }

    [Fact]
    public async Task UpdateSuite_still_applies_an_explicit_agentId_and_name()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = new AgentTestSuite { Id = "suite-1", AgentId = "agent-1", Name = "old", Enabled = true };
        var controller = BuildController(repo, out _);

        var request = new AgentTestSuiteUpsertRequest { AgentId = "agent-2", Name = "new" };

        await controller.UpdateSuite("suite-1", request);

        Assert.Equal("agent-2", repo.Suites["suite-1"].AgentId);
        Assert.Equal("new", repo.Suites["suite-1"].Name);
    }

    // ---- Coordinator re-review item 2: a partial UpdateSuite must not silently re-enable a
    // suite someone deliberately disabled (Enabled is now bool?; null means "omitted") ----------

    [Fact]
    public async Task UpdateSuite_preserves_a_disabled_suite_when_the_request_omits_enabled()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = new AgentTestSuite
        {
            Id = "suite-1", AgentId = "agent-1", Name = "s", Enabled = false
        };
        var controller = BuildController(repo, out _);

        // A partial body that only means to change CaseTimeoutSeconds -- Enabled is omitted
        // (null), not explicitly re-enabled. Before item 2's fix, AgentTestSuiteUpsertRequest.
        // Enabled was a non-nullable bool defaulting to true, so this exact request would have
        // silently flipped the suite back on.
        var request = new AgentTestSuiteUpsertRequest
        {
            AgentId = "agent-1", Name = "s", CaseTimeoutSeconds = 30
        };

        var result = await controller.UpdateSuite("suite-1", request);

        Assert.Null(result.Result);
        Assert.False(repo.Suites["suite-1"].Enabled);
        Assert.Equal(30, repo.Suites["suite-1"].CaseTimeoutSeconds);
    }

    [Fact]
    public async Task UpdateSuite_still_applies_an_explicit_enabled_override()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = new AgentTestSuite
        {
            Id = "suite-1", AgentId = "agent-1", Name = "s", Enabled = false
        };
        var controller = BuildController(repo, out _);

        var request = new AgentTestSuiteUpsertRequest { AgentId = "agent-1", Name = "s", Enabled = true };

        await controller.UpdateSuite("suite-1", request);

        Assert.True(repo.Suites["suite-1"].Enabled);
    }

    [Fact]
    public async Task UpdateSuite_can_still_explicitly_disable_an_enabled_suite()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = EnabledSuite();
        var controller = BuildController(repo, out _);

        var request = new AgentTestSuiteUpsertRequest { AgentId = "agent-1", Name = "s", Enabled = false };

        await controller.UpdateSuite("suite-1", request);

        Assert.False(repo.Suites["suite-1"].Enabled);
    }

    [Fact]
    public async Task CreateSuite_defaults_to_enabled_when_the_request_omits_it()
    {
        var repo = new InMemoryRepo();
        var controller = BuildController(repo, out _);

        var result = await controller.CreateSuite(new AgentTestSuiteUpsertRequest { AgentId = "agent-1", Name = "s" });

        Assert.True(result.Enabled);
    }

    [Fact]
    public async Task CreateSuite_honors_an_explicit_disabled_flag()
    {
        var repo = new InMemoryRepo();
        var controller = BuildController(repo, out _);

        var result = await controller.CreateSuite(
            new AgentTestSuiteUpsertRequest { AgentId = "agent-1", Name = "s", Enabled = false });

        Assert.False(result.Enabled);
    }

    // ---- Item 8c: cancelling an already-terminal run is rejected ----------------------------

    [Theory]
    [InlineData(AgentTestStatus.Passed)]
    [InlineData(AgentTestStatus.Failed)]
    [InlineData(AgentTestStatus.Error)]
    [InlineData(AgentTestStatus.Cancelled)]
    public async Task CancelRun_returns_conflict_for_a_run_that_already_finished(string terminalStatus)
    {
        var repo = new InMemoryRepo();
        repo.Runs["run-1"] = new AgentTestRun { Id = "run-1", SuiteId = "suite-1", Status = terminalStatus };
        var controller = BuildController(repo, out _);

        var result = await controller.CancelRun("run-1");

        Assert.IsType<ConflictObjectResult>(result);
        Assert.False(repo.Runs["run-1"].CancelRequested);
    }

    [Theory]
    [InlineData(AgentTestStatus.Pending)]
    [InlineData(AgentTestStatus.Running)]
    public async Task CancelRun_still_accepts_a_run_that_has_not_finished(string liveStatus)
    {
        var repo = new InMemoryRepo();
        repo.Runs["run-1"] = new AgentTestRun { Id = "run-1", SuiteId = "suite-1", Status = liveStatus };
        var controller = BuildController(repo, out _);

        var result = await controller.CancelRun("run-1");

        Assert.IsType<OkResult>(result);
        Assert.True(repo.Runs["run-1"].CancelRequested);
    }
}
