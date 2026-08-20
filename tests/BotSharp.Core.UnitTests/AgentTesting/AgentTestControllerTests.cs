using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
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

    /// <summary>
    /// An IAgentService that resolves exactly the given ids. The default Mock.Of&lt;IAgentService&gt;
    /// resolves nothing, which is correct for every test that leaves EntryAgentId blank (the check
    /// short-circuits there) but would reject any test that sets one.
    /// </summary>
    private static IAgentService AgentServiceKnowing(params string[] agentIds)
    {
        var known = new HashSet<string>(agentIds, StringComparer.OrdinalIgnoreCase);
        var mock = new Mock<IAgentService>();
        mock.Setup(x => x.GetAgent(It.IsAny<string>()))
            .ReturnsAsync((string id) => known.Contains(id) ? new Agent { Id = id, Name = id } : null!);
        return mock.Object;
    }

    private static AgentTestController BuildController(
        InMemoryRepo repo,
        out RecordingQueue queue,
        ILlmProviderService? llmProviders = null,
        IAgentService? agents = null)
    {
        var recorder = new AgentTestRecorder(
            Mock.Of<IBotSharpRepository>(),
            repo,
            NullLogger<AgentTestRecorder>.Instance);
        queue = new RecordingQueue();

        var controller = new AgentTestController(
            repo, queue, agents ?? Mock.Of<IAgentService>(), recorder, llmProviders ?? ProviderServiceKnowing());

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

    private static AgentTestCaseUpsertRequest RoutingCaseRequest(
        int turns = 1, params TestAssertion[] assertions) => new()
    {
        SuiteId = "suite-1",
        Name = "routing case",
        CaseType = CaseTypes.Routing,
        Turns = Enumerable.Range(0, turns)
            .Select(i => new TestTurn { Index = i, UserMessage = "hi" })
            .ToList(),
        Assertions = assertions.ToList()
    };

    private static InMemoryRepo RepoWithSuite()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = new AgentTestSuite { Id = "suite-1", AgentId = "agent-1", Name = "s" };
        return repo;
    }

    [Fact]
    public async Task An_unknown_case_type_is_rejected_rather_than_stored_as_the_default()
    {
        // Silently storing "Rounting" as Agent would leave the author with a case that reads
        // routing-shaped in the UI and is never counted towards routing accuracy -- the failure mode
        // is a gate figure quietly measuring fewer cases than anyone thinks.
        var controller = BuildController(RepoWithSuite(), out _);

        var request = RoutingCaseRequest(
            assertions: new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" });
        request.CaseType = "Rounting";

        var response = await controller.CreateCase(request);

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("caseType", bad.Value?.ToString());
    }

    [Theory]
    [InlineData("routing")]
    [InlineData("ROUTING")]
    public async Task A_case_type_is_stored_in_its_canonical_casing(string sent)
    {
        // Every comparison against CaseTypes.Routing is Ordinal, so storing the caller's casing would
        // leave a case that never matches -- it would run, and then not be counted.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        var request = RoutingCaseRequest(
            assertions: new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" });
        request.CaseType = sent;

        await controller.CreateCase(request);

        Assert.Equal(CaseTypes.Routing, Assert.Single(repo.Cases.Values).CaseType);
    }

    [Fact]
    public async Task A_case_that_omits_the_type_is_an_agent_case()
    {
        // Backward compatibility for every client written before the field existed, and for the
        // documents already in the store: both have to keep meaning Agent.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        Assert.Equal(CaseTypes.Agent, Assert.Single(repo.Cases.Values).CaseType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task A_routing_case_must_have_exactly_one_turn(int turns)
    {
        // Routing is a single-turn question: which agent picks this message up. A second turn asks
        // something else, and its verdict would still land in the routing accuracy figure.
        var controller = BuildController(RepoWithSuite(), out _);

        var response = await controller.CreateCase(RoutingCaseRequest(
            turns, new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" }));

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("exactly one turn", bad.Value?.ToString());
    }

    [Fact]
    public async Task A_routing_case_without_a_routing_assertion_is_rejected()
    {
        // Otherwise the case counts towards routing accuracy while asserting nothing about routing:
        // it reports Passed for having successfully said anything at all.
        var controller = BuildController(RepoWithSuite(), out _);

        var response = await controller.CreateCase(RoutingCaseRequest(
            assertions: new TestAssertion { Type = AssertionTypes.OutputContains, Expected = "hello" }));

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains(AssertionTypes.RoutedToAgent, bad.Value?.ToString());
    }

    [Fact]
    public async Task Either_routing_assertion_type_satisfies_that_requirement()
    {
        // agentChain is the assertion that can actually describe a hand-off, so it has to count --
        // requiring routedToAgent specifically would force every routing case onto the weaker one.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        var response = await controller.CreateCase(RoutingCaseRequest(
            assertions: new TestAssertion
            {
                Type = AssertionTypes.AgentChain,
                Target = AgentChainModes.Exact,
                Expected = "Work Order Creator"
            }));

        Assert.Null(response.Result);
        Assert.Equal(CaseTypes.Routing, Assert.Single(repo.Cases.Values).CaseType);
    }

    [Fact]
    public async Task A_turn_level_routing_assertion_counts_too()
    {
        // A single-turn routing case can just as reasonably put its assertion on the turn as at case
        // level; looking only at case-level assertions would reject a perfectly good case.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        var response = await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            CaseType = CaseTypes.Routing,
            Turns =
            [
                new TestTurn
                {
                    Index = 0,
                    UserMessage = "hi",
                    Assertions = [new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" }]
                }
            ]
        });

        Assert.Null(response.Result);
        Assert.Single(repo.Cases.Values);
    }

    [Fact]
    public async Task A_routing_case_cannot_use_the_llm_judge()
    {
        // Routing is scored purely as expected-agent == actual-agent. An llmJudge would also make the
        // routing figure depend on a vendor call, so a vendor outage would read as a routing
        // regression.
        var controller = BuildController(RepoWithSuite(), out _);

        var response = await controller.CreateCase(RoutingCaseRequest(
            1,
            new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" },
            new TestAssertion { Type = AssertionTypes.LlmJudge, Expected = "is polite", MinScore = 4 }));

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains(AssertionTypes.LlmJudge, bad.Value?.ToString());
    }

    [Fact]
    public async Task An_agent_case_is_not_held_to_the_routing_rules()
    {
        // Those rules are Routing-only. An Agent case is normally multi-turn and may well use
        // llmJudge; applying routing's constraints to it would break every case already stored.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        var response = await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            CaseType = CaseTypes.Agent,
            Turns =
            [
                new TestTurn { Index = 0, UserMessage = "a" },
                new TestTurn { Index = 1, UserMessage = "b" }
            ],
            Assertions = [new TestAssertion { Type = AssertionTypes.LlmJudge, Expected = "is polite", MinScore = 4 }]
        });

        Assert.Null(response.Result);
        Assert.Single(repo.Cases.Values);
    }

    [Fact]
    public async Task An_entry_agent_that_does_not_exist_is_rejected_at_save_time()
    {
        // A typo here would otherwise turn every run of the case into an opaque infrastructure Error:
        // the canary fails against an agent BotSharp cannot load, and its message says nothing about
        // the real cause.
        var controller = BuildController(RepoWithSuite(), out _, agents: AgentServiceKnowing("copilot-entry"));

        var request = RoutingCaseRequest(
            assertions: new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" });
        request.EntryAgentId = "coplot-entry";

        var response = await controller.CreateCase(request);

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("coplot-entry", bad.Value?.ToString());
    }

    [Fact]
    public async Task A_known_entry_agent_is_stored_trimmed()
    {
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _, agents: AgentServiceKnowing("copilot-entry"));

        var request = RoutingCaseRequest(
            assertions: new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" });
        request.EntryAgentId = "  copilot-entry  ";

        await controller.CreateCase(request);

        Assert.Equal("copilot-entry", Assert.Single(repo.Cases.Values).EntryAgentId);
    }

    [Fact]
    public async Task A_blank_entry_agent_is_stored_as_null_and_needs_no_lookup()
    {
        // Null is what the runner's "fall back to the suite's agent" check reads. A UI posting "" for
        // an untouched field must not retarget the case at an agent id of "", and saving a case must
        // not require knowing any agent id at all -- note this controller resolves no agents.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        var request = RoutingCaseRequest(
            assertions: new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" });
        request.EntryAgentId = "   ";

        await controller.CreateCase(request);

        Assert.Null(Assert.Single(repo.Cases.Values).EntryAgentId);
    }

    [Fact]
    public async Task E2E_is_no_longer_an_accepted_case_type()
    {
        // Dropped by project owner decision: a multi-agent journey is an Agent case whose agentChain
        // assertion describes the hand-offs, so a third type bought nothing but a third branch in
        // every validation and aggregation path.
        var controller = BuildController(RepoWithSuite(), out _);

        var request = RoutingCaseRequest(
            assertions: new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" });
        request.CaseType = "E2E";

        var response = await controller.CreateCase(request);

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("caseType", bad.Value?.ToString());
    }

    [Theory]
    [InlineData("system")]
    [InlineData("function")]
    [InlineData("tool")]
    [InlineData("")]
    public async Task An_unsupported_history_role_is_rejected(string role)
    {
        // system would compete with the agent's own instruction, and function would fake a tool call
        // -- letting a case claim a tool ran when nothing did. Both are worse than a save error.
        var controller = BuildController(RepoWithSuite(), out _);

        var response = await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            History = [new TestHistoryMessage { Role = role, Content = "hello" }],
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("history message 1", bad.Value?.ToString());
    }

    [Fact]
    public async Task A_history_message_with_no_content_is_rejected()
    {
        // BotSharp's own dialog storage drops elements with blank content, so it would not be there
        // at run time, and the runner's write-count check would then fail the case with a confusing
        // message about the write having vanished.
        var controller = BuildController(RepoWithSuite(), out _);

        var response = await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            History = [new TestHistoryMessage { Role = HistoryRoles.User, Content = "   " }],
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("no content", bad.Value?.ToString());
    }

    [Fact]
    public async Task A_history_role_is_stored_in_its_canonical_casing()
    {
        // The driver and every later comparison use the lowercase constants.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            History = [new TestHistoryMessage { Role = "ASSISTANT", Content = "hello" }],
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        var stored = Assert.Single(repo.Cases.Values);
        Assert.Equal(HistoryRoles.Assistant, Assert.Single(stored.History).Role);
    }

    [Fact]
    public async Task History_does_not_count_towards_a_routing_cases_one_turn_limit()
    {
        // Replaying a prior exchange and then asking one question is still a single routing decision,
        // and it is the most realistic way to test routing that depends on context. Counting history
        // as turns would make that impossible to express.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        var request = RoutingCaseRequest(
            assertions: new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO" });
        request.History =
        [
            new TestHistoryMessage { Role = HistoryRoles.User, Content = "my fridge is leaking" },
            new TestHistoryMessage { Role = HistoryRoles.Assistant, Content = "I raised work order B123." }
        ];

        var response = await controller.CreateCase(request);

        Assert.Null(response.Result);
        Assert.Equal(2, Assert.Single(repo.Cases.Values).History.Count);
    }

    /// <summary>
    /// A case with every field populated, so a copy test fails when a field is dropped rather than
    /// only when the obvious ones are.
    /// </summary>
    private static AgentTestCase FullyPopulatedCase() => new()
    {
        Id = "case-1",
        SuiteId = "suite-1",
        Name = "asking for an ETA",
        Enabled = true,
        CaseType = CaseTypes.Routing,
        EntryAgentId = "copilot-entry",
        History = [new TestHistoryMessage { Role = HistoryRoles.User, Content = "my fridge is leaking" }],
        Turns =
        [
            new TestTurn
            {
                Index = 0,
                UserMessage = "when is someone coming?",
                Assertions = [new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO", Fatal = true }]
            }
        ],
        Assertions =
        [
            new TestAssertion
            {
                Type = AssertionTypes.AgentChain,
                Target = AgentChainModes.Ordered,
                Expected = "Copilot, WO"
            }
        ],
        InitialStates = [new TestState { Key = "wo_num", Value = "B123", ActiveRounds = 3, Global = true }],
        Mocks =
        [
            new TestToolMock
            {
                FunctionName = "get_estimate_arrival_time",
                ArgsMatchJson = "{\"wo_num\":\"B123\"}",
                CallIndex = 1,
                ResultContent = "tomorrow 9am",
                StopCompletion = true,
                StateWrites = [new TestState { Key = "eta", Value = "tomorrow" }]
            }
        ],
        UnmockedToolPolicy = UnmockedToolPolicies.Block,
        SourceConversationId = "conv-9"
    };

    [Fact]
    public async Task Copying_a_case_carries_every_field()
    {
        // The reason this endpoint exists server-side at all. A client that rebuilds the payload from
        // its own form drops whatever it does not know about, and a copy missing its mocks looks
        // identical in the list right up to the run where it blocks every tool.
        var repo = RepoWithSuite();
        var source = FullyPopulatedCase();
        repo.Cases[source.Id] = source;
        var controller = BuildController(repo, out _);

        var response = await controller.CopyCase("case-1");

        var copy = Assert.IsType<AgentTestCase>(response.Value);
        Assert.Equal(source.SuiteId, copy.SuiteId);
        Assert.Equal(source.CaseType, copy.CaseType);
        Assert.Equal(source.EntryAgentId, copy.EntryAgentId);
        Assert.Equal(source.UnmockedToolPolicy, copy.UnmockedToolPolicy);
        Assert.Equal(source.SourceConversationId, copy.SourceConversationId);

        Assert.Equal("my fridge is leaking", Assert.Single(copy.History).Content);
        Assert.Equal("when is someone coming?", Assert.Single(copy.Turns).UserMessage);
        Assert.True(Assert.Single(Assert.Single(copy.Turns).Assertions).Fatal);
        Assert.Equal(AgentChainModes.Ordered, Assert.Single(copy.Assertions).Target);

        var state = Assert.Single(copy.InitialStates);
        Assert.Equal("wo_num", state.Key);
        Assert.Equal(3, state.ActiveRounds);
        Assert.True(state.Global);

        var mock = Assert.Single(copy.Mocks);
        Assert.Equal("get_estimate_arrival_time", mock.FunctionName);
        Assert.Equal("{\"wo_num\":\"B123\"}", mock.ArgsMatchJson);
        Assert.Equal(1, mock.CallIndex);
        Assert.True(mock.StopCompletion);
        Assert.Equal("eta", Assert.Single(mock.StateWrites!).Key);
    }

    [Fact]
    public async Task A_copy_lands_disabled_even_when_the_source_was_enabled()
    {
        // An exact duplicate joining the next run measures the same thing twice: it pads the
        // pass-rate denominator, and for a routing case it double-weights one routing decision. The
        // copy waits for the edit it was made for.
        var repo = RepoWithSuite();
        repo.Cases["case-1"] = FullyPopulatedCase();
        var controller = BuildController(repo, out _);

        var response = await controller.CopyCase("case-1");

        Assert.False(Assert.IsType<AgentTestCase>(response.Value).Enabled);
        // And the source is untouched.
        Assert.True(repo.Cases["case-1"].Enabled);
    }

    [Fact]
    public async Task A_copy_gets_its_own_id_and_does_not_overwrite_the_source()
    {
        // The BSON round trip copies the source's _id, so failing to blank it would turn the copy
        // into a full overwrite of the original -- the worst possible outcome for a copy button.
        var repo = RepoWithSuite();
        repo.Cases["case-1"] = FullyPopulatedCase();
        var controller = BuildController(repo, out _);

        var response = await controller.CopyCase("case-1");

        var copy = Assert.IsType<AgentTestCase>(response.Value);
        Assert.NotEqual("case-1", copy.Id);
        Assert.NotEmpty(copy.Id);
        Assert.Equal(2, repo.Cases.Count);
        Assert.Equal("asking for an ETA", repo.Cases["case-1"].Name);
    }

    [Fact]
    public async Task Editing_a_copy_does_not_reach_back_into_the_source()
    {
        // A shallow clone would have both documents sharing the same Turns/Mocks list instances, so
        // the first edit to the copy would silently rewrite the case it came from.
        var repo = RepoWithSuite();
        repo.Cases["case-1"] = FullyPopulatedCase();
        var controller = BuildController(repo, out _);

        var copy = Assert.IsType<AgentTestCase>((await controller.CopyCase("case-1")).Value);

        copy.Turns[0].UserMessage = "changed";
        copy.Mocks[0].ResultContent = "changed";
        copy.History[0].Content = "changed";

        var source = repo.Cases["case-1"];
        Assert.Equal("when is someone coming?", source.Turns[0].UserMessage);
        Assert.Equal("tomorrow 9am", source.Mocks[0].ResultContent);
        Assert.Equal("my fridge is leaking", source.History[0].Content);
    }

    [Fact]
    public async Task Repeated_copies_get_distinguishable_names()
    {
        // Two rows both called "x (copy)" cannot be told apart in the list, which is the one place
        // copies are managed.
        var repo = RepoWithSuite();
        repo.Cases["case-1"] = FullyPopulatedCase();
        var controller = BuildController(repo, out _);

        var first = Assert.IsType<AgentTestCase>((await controller.CopyCase("case-1")).Value);
        var second = Assert.IsType<AgentTestCase>((await controller.CopyCase("case-1")).Value);
        var third = Assert.IsType<AgentTestCase>((await controller.CopyCase("case-1")).Value);

        Assert.Equal("asking for an ETA (copy)", first.Name);
        Assert.Equal("asking for an ETA (copy 2)", second.Name);
        Assert.Equal("asking for an ETA (copy 3)", third.Name);
    }

    [Fact]
    public async Task A_copy_of_a_copy_is_named_from_the_case_it_was_copied_from()
    {
        // Not "x (copy) (copy)": the suffix is appended to whatever the source is called, and the
        // collision check is what keeps the result unique.
        var repo = RepoWithSuite();
        repo.Cases["case-1"] = FullyPopulatedCase();
        var controller = BuildController(repo, out _);

        var first = Assert.IsType<AgentTestCase>((await controller.CopyCase("case-1")).Value);
        var nested = Assert.IsType<AgentTestCase>((await controller.CopyCase(first.Id)).Value);

        Assert.Equal("asking for an ETA (copy) (copy)", nested.Name);
    }

    [Fact]
    public async Task A_copied_name_stays_short_enough_to_edit()
    {
        // A name the case editor's own input cannot hold would have to be trimmed by hand before any
        // other change to the copy could be saved.
        var repo = RepoWithSuite();
        var source = FullyPopulatedCase();
        source.Name = new string('x', 200);
        repo.Cases["case-1"] = source;
        var controller = BuildController(repo, out _);

        var copy = Assert.IsType<AgentTestCase>((await controller.CopyCase("case-1")).Value);

        Assert.True(copy.Name.Length <= 200, $"name was {copy.Name.Length} characters");
        Assert.EndsWith(" (copy)", copy.Name);
    }

    [Fact]
    public async Task A_copy_does_not_inherit_the_sources_create_date()
    {
        // The round trip copies it, which would have the copy claim to be as old as the case it came
        // from -- and the case list is sorted newest first, so a fresh copy would appear buried.
        var repo = RepoWithSuite();
        var source = FullyPopulatedCase();
        source.CreateDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        repo.Cases["case-1"] = source;
        var controller = BuildController(repo, out _);

        var copy = Assert.IsType<AgentTestCase>((await controller.CopyCase("case-1")).Value);

        Assert.True(copy.CreateDate > new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Copying_a_case_that_does_not_exist_is_a_404()
    {
        var controller = BuildController(RepoWithSuite(), out _);

        var response = await controller.CopyCase("nope");

        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    // ------------------------------------------------------------- governance metadata

    [Theory]
    [InlineData("P3")]
    [InlineData("high")]
    public async Task An_unknown_priority_is_rejected(string priority)
    {
        // Priority decides the batch, and a batch decides whether a failure stops the evaluation.
        // Storing "high" would leave a case that matches no priority and lands in the default batch
        // while its author believes it is stop-loss.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        var response = await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            Priority = priority,
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("priority", bad.Value?.ToString());
    }

    [Fact]
    public async Task An_unknown_severity_is_rejected()
    {
        var controller = BuildController(RepoWithSuite(), out _);

        var response = await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            Severity = "critical",
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("severity", bad.Value?.ToString());
    }

    [Fact]
    public async Task An_out_of_range_batch_is_rejected_rather_than_clamped()
    {
        // Clamping 4 to 3 would file the case somewhere its author never asked for, silently, in the
        // batch that does not block a release.
        var controller = BuildController(RepoWithSuite(), out _);

        var response = await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            Batch = 4,
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        var bad = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Contains("batch", bad.Value?.ToString());
    }

    [Fact]
    public async Task Priority_and_severity_are_stored_in_canonical_casing()
    {
        // Every comparison against CasePriorities.P0 is Ordinal, so storing "p0" would leave a case
        // that runs and is then filed in the wrong batch.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            Priority = "p0",
            Severity = "s0",
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        var stored = Assert.Single(repo.Cases.Values);
        Assert.Equal(CasePriorities.P0, stored.Priority);
        Assert.Equal(CaseSeverities.S0, stored.Severity);
    }

    [Fact]
    public async Task A_case_that_omits_the_governance_fields_gets_the_untriaged_defaults()
    {
        // P1 and S1 for every case stored before these fields existed: P0/S0 would make each of them
        // an immediate no-go, and P2/S2 would drop them out of the mandatory batches and let a real
        // failure read as an experience nit.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        var stored = Assert.Single(repo.Cases.Values);
        Assert.Equal(CasePriorities.P1, stored.Priority);
        Assert.Equal(CaseSeverities.S1, stored.Severity);
        Assert.Null(stored.Batch);
        Assert.False(stored.CrossCutting);
        Assert.Empty(stored.InvolvedAgents);
        Assert.Null(stored.LastReviewedDate);
    }

    [Fact]
    public async Task Involved_agents_are_trimmed_and_deduplicated()
    {
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            InvolvedAgents = ["  agent-a  ", "AGENT-A", "", "agent-b"],
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        Assert.Equal(["agent-a", "agent-b"], Assert.Single(repo.Cases.Values).InvolvedAgents);
    }

    [Fact]
    public async Task Saving_a_case_never_stamps_the_reviewed_date()
    {
        // A case can be edited many times and still rest on an assumption nobody has questioned in a
        // year. Stamping this on every write would hide exactly that.
        var repo = RepoWithSuite();
        var controller = BuildController(repo, out _);

        await controller.CreateCase(new AgentTestCaseUpsertRequest
        {
            SuiteId = "suite-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        });

        Assert.Null(Assert.Single(repo.Cases.Values).LastReviewedDate);
    }

    // ------------------------------------------------------------- scope selection

    private static InMemoryRepo RepoWithScopedCases()
    {
        var repo = new InMemoryRepo();
        repo.Suites["suite-1"] = new AgentTestSuite { Id = "suite-1", AgentId = "agent-a", Name = "A suite" };
        repo.Cases["on-target"] = new AgentTestCase
        {
            Id = "on-target", SuiteId = "suite-1", Name = "on target", EntryAgentId = "agent-a"
        };
        repo.Cases["off-target"] = new AgentTestCase
        {
            Id = "off-target", SuiteId = "suite-1", Name = "off target", EntryAgentId = "agent-b"
        };
        repo.Cases["safety"] = new AgentTestCase
        {
            Id = "safety", SuiteId = "suite-1", Name = "safety", EntryAgentId = "agent-b", CrossCutting = true
        };
        repo.Cases["draft"] = new AgentTestCase
        {
            Id = "draft", SuiteId = "suite-1", Name = "draft", EntryAgentId = "agent-a", Enabled = false
        };
        return repo;
    }

    [Fact]
    public async Task A_scope_with_no_targets_and_no_platform_flag_is_rejected()
    {
        // It would narrow to nothing, and an empty scope reported as a successful plan is the single
        // most dangerous answer this endpoint could give: it reads as "nothing needs testing".
        var controller = BuildController(RepoWithScopedCases(), out _);

        var response = await controller.SelectScope(new ScopeSelectionRequest());

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task A_scope_reports_both_halves_with_a_reason_for_each()
    {
        // The excluded half is the one worth reading: an excluded case produces no result to notice,
        // so the only defence is being able to see what was left out and why.
        var controller = BuildController(RepoWithScopedCases(), out _);

        var response = await controller.SelectScope(new ScopeSelectionRequest
        {
            TargetAgentIds = ["agent-a"]
        });

        var scope = Assert.IsType<ScopeSelectionResponse>(response.Value);
        Assert.Equal(4, scope.TotalCases);

        Assert.Equal(
            ["on-target", "safety"],
            scope.Included.Select(c => c.CaseId).OrderBy(id => id).ToList());
        Assert.Equal(ScopeReasons.TargetAgent, scope.Included.Single(c => c.CaseId == "on-target").Reason);
        Assert.Equal(ScopeReasons.CrossCutting, scope.Included.Single(c => c.CaseId == "safety").Reason);

        Assert.Equal(ScopeReasons.NotInvolved, scope.Excluded.Single(c => c.CaseId == "off-target").Reason);
        Assert.Equal(ScopeReasons.Disabled, scope.Excluded.Single(c => c.CaseId == "draft").Reason);
    }

    [Fact]
    public async Task A_platform_wide_scope_includes_every_enabled_case()
    {
        // Narrowing switches off: there is no agent a foundation model swap demonstrably does not
        // touch. Disabled cases stay out, because no run would execute them.
        var controller = BuildController(RepoWithScopedCases(), out _);

        var response = await controller.SelectScope(new ScopeSelectionRequest { FullPlatform = true });

        var scope = Assert.IsType<ScopeSelectionResponse>(response.Value);
        Assert.Equal(3, scope.Included.Count);
        Assert.Equal("draft", Assert.Single(scope.Excluded).CaseId);
    }

    [Fact]
    public async Task A_scope_carries_the_metadata_the_decision_was_made_from()
    {
        // A verdict on its own cannot be reviewed. The involved set and the effective batch are what
        // let someone check the plan rather than trust it.
        var controller = BuildController(RepoWithScopedCases(), out _);

        var response = await controller.SelectScope(new ScopeSelectionRequest
        {
            TargetAgentIds = ["agent-a"]
        });

        var scope = Assert.IsType<ScopeSelectionResponse>(response.Value);
        var onTarget = scope.Included.Single(c => c.CaseId == "on-target");

        Assert.Equal(["agent-a"], onTarget.InvolvedAgentIds);
        Assert.Equal(CaseBatches.Mandatory, onTarget.Batch);
        Assert.Equal("A suite", onTarget.SuiteName);
        // Cross-cutting forces batch 1, and the response has to agree with that.
        Assert.Equal(CaseBatches.StopLoss, scope.Included.Single(c => c.CaseId == "safety").Batch);
    }

    [Fact]
    public async Task A_scope_can_be_narrowed_to_one_batch()
    {
        var controller = BuildController(RepoWithScopedCases(), out _);

        var response = await controller.SelectScope(new ScopeSelectionRequest
        {
            TargetAgentIds = ["agent-a"],
            Batch = CaseBatches.StopLoss
        });

        var scope = Assert.IsType<ScopeSelectionResponse>(response.Value);
        Assert.Equal("safety", Assert.Single(scope.Included).CaseId);
        Assert.Equal(ScopeReasons.OtherBatch, scope.Excluded.Single(c => c.CaseId == "on-target").Reason);
    }

    [Fact]
    public async Task An_out_of_range_batch_on_a_scope_is_rejected()
    {
        var controller = BuildController(RepoWithScopedCases(), out _);

        var response = await controller.SelectScope(new ScopeSelectionRequest
        {
            TargetAgentIds = ["agent-a"],
            Batch = 7
        });

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }
}
