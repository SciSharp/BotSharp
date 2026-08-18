using System.Security.Claims;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Infrastructures.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BotSharp.Plugin.AgentTesting.Models;
using BotSharp.Plugin.AgentTesting.Repositories;
using BotSharp.Plugin.AgentTesting.Services;

namespace BotSharp.Plugin.AgentTesting.Controllers;

/// <summary>
/// CRUD for suites and cases, triggering a run (asynchronous -- returns the runId immediately),
/// reading or cancelling a run, and the mock-target candidate list the case editor needs. All
/// literal absolute routes, all requiring authentication.
/// </summary>
[Authorize]
[ApiController]
[Route("agent-test")]
public class AgentTestController : ControllerBase
{
    private readonly IAgentTestRepository _repo;
    private readonly IAgentTestRunQueue _queue;
    private readonly IAgentService _agents;
    private readonly AgentTestRecorder _recorder;
    private readonly ILlmProviderService _llmProviders;

    public AgentTestController(
        IAgentTestRepository repo,
        IAgentTestRunQueue queue,
        IAgentService agents,
        AgentTestRecorder recorder,
        ILlmProviderService llmProviders)
    {
        _repo = repo;
        _queue = queue;
        _agents = agents;
        _recorder = recorder;
        _llmProviders = llmProviders;
    }

    [HttpGet("suites")]
    public async Task<List<AgentTestSuite>> ListSuites([FromQuery] string? agentId)
        => await _repo.ListSuitesAsync(agentId);

    [HttpPost("suites")]
    public async Task<AgentTestSuite> CreateSuite([FromBody] AgentTestSuiteUpsertRequest request)
    {
        var suite = new AgentTestSuite();
        ApplySuite(suite, request);

        await _repo.UpsertSuiteAsync(suite);
        return suite;
    }

    [HttpGet("suites/{id}")]
    public async Task<ActionResult<AgentTestSuite>> GetSuite(string id)
    {
        var suite = await _repo.GetSuiteAsync(id);
        if (suite == null)
        {
            return NotFound($"agent test suite {id} not found");
        }

        return suite;
    }

    [HttpPut("suites/{id}")]
    public async Task<ActionResult<AgentTestSuite>> UpdateSuite(string id, [FromBody] AgentTestSuiteUpsertRequest request)
    {
        var suite = await _repo.GetSuiteAsync(id);
        if (suite == null)
        {
            return NotFound($"agent test suite {id} not found");
        }

        // A blank agentId/name in the request means "leave it where it is," not "clear it" --
        // ApplySuite below copies every field unconditionally (matching CreateSuite's full-replace
        // semantics), and without this fallback a PUT body that omits either field would silently
        // blank it. Mirrors UpdateCase's SuiteId fallback below.
        if (string.IsNullOrWhiteSpace(request.AgentId))
        {
            request.AgentId = suite.AgentId;
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            request.Name = suite.Name;
        }

        ApplySuite(suite, request);

        await _repo.UpsertSuiteAsync(suite);
        return suite;
    }

    [HttpDelete("suites/{id}")]
    public async Task<IActionResult> DeleteSuite(string id)
    {
        var suite = await _repo.GetSuiteAsync(id);
        if (suite == null)
        {
            return NotFound($"agent test suite {id} not found");
        }

        await _repo.DeleteSuiteAsync(id);
        return Ok();
    }

    [HttpGet("cases")]
    public async Task<ActionResult<List<AgentTestCase>>> ListCases([FromQuery] string? suiteId)
    {
        if (string.IsNullOrWhiteSpace(suiteId))
        {
            return BadRequest("suiteId is required");
        }

        return await _repo.ListCasesAsync(suiteId);
    }

    [HttpPost("cases")]
    public async Task<ActionResult<AgentTestCase>> CreateCase([FromBody] AgentTestCaseUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SuiteId))
        {
            return BadRequest("suiteId is required");
        }

        if (ValidateCasePayload(request) is { } validationError)
        {
            return BadRequest(validationError);
        }

        var suite = await _repo.GetSuiteAsync(request.SuiteId);
        if (suite == null)
        {
            return NotFound($"agent test suite {request.SuiteId} not found");
        }

        var testCase = new AgentTestCase();
        ApplyCase(testCase, request);

        await _repo.UpsertCaseAsync(testCase);
        return testCase;
    }

    [HttpGet("cases/{id}")]
    public async Task<ActionResult<AgentTestCase>> GetCase(string id)
    {
        var testCase = await _repo.GetCaseAsync(id);
        if (testCase == null)
        {
            return NotFound($"agent test case {id} not found");
        }

        return testCase;
    }

    [HttpPut("cases/{id}")]
    public async Task<ActionResult<AgentTestCase>> UpdateCase(string id, [FromBody] AgentTestCaseUpsertRequest request)
    {
        var testCase = await _repo.GetCaseAsync(id);
        if (testCase == null)
        {
            return NotFound($"agent test case {id} not found");
        }

        if (ValidateCasePayload(request) is { } validationError)
        {
            return BadRequest(validationError);
        }

        // A blank SuiteId in the request means "leave it where it is" -- ApplyCase below copies
        // every field unconditionally (matching CreateCase's full-replace semantics), and without
        // this fallback a PUT body that omits suiteId would silently clear it to string.Empty,
        // orphaning the case from ListCasesAsync(suiteId) queries. Only re-validate existence when
        // the target actually differs from what the case already points at.
        var targetSuiteId = string.IsNullOrWhiteSpace(request.SuiteId) ? testCase.SuiteId : request.SuiteId;
        if (targetSuiteId != testCase.SuiteId)
        {
            var suite = await _repo.GetSuiteAsync(targetSuiteId);
            if (suite == null)
            {
                return NotFound($"agent test suite {targetSuiteId} not found");
            }
        }

        request.SuiteId = targetSuiteId;
        ApplyCase(testCase, request);

        await _repo.UpsertCaseAsync(testCase);
        return testCase;
    }

    [HttpDelete("cases/{id}")]
    public async Task<IActionResult> DeleteCase(string id)
    {
        var testCase = await _repo.GetCaseAsync(id);
        if (testCase == null)
        {
            return NotFound($"agent test case {id} not found");
        }

        await _repo.DeleteCaseAsync(id);
        return Ok();
    }

    /// <summary>
    /// Records a draft case from a real conversation: real function returns become mocks, real
    /// state deltas become StateWrites/InitialStates, and the stable assertions
    /// (toolCalled/stateEquals) are generated -- so nobody has to hand-write a work order agent's
    /// mock JSON. The draft is stored with Enabled = false and has to be reviewed and enabled by
    /// hand before it joins a real run.
    ///
    /// [BotSharpAuth]: this endpoint copies the raw contents of a real conversation (potentially
    /// phone numbers, addresses, tenant names) into the test store, and the conversationId comes
    /// from the caller with no ownership check -- the highest PII-escalation risk on this whole
    /// surface. Restricted to admin/root rather than any authenticated user being able to call it
    /// against any conversation.
    /// </summary>
    [BotSharpAuth]
    [HttpPost("record")]
    public async Task<ActionResult<List<AgentTestCase>>> RecordCase([FromBody] AgentTestRecordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SuiteId) || string.IsNullOrWhiteSpace(request.ConversationId))
        {
            return BadRequest("suiteId and conversationId are required");
        }

        // Same guard as TriggerRun: a model this host cannot run must fail here with a readable
        // message, not deep inside the segmenter's completion call.
        if (ValidateRequestedModels(request.Model == null ? null : [request.Model]) is { } modelError)
        {
            return BadRequest(modelError);
        }

        var suite = await _repo.GetSuiteAsync(request.SuiteId);
        if (suite == null)
        {
            return NotFound($"agent test suite {request.SuiteId} not found");
        }

        try
        {
            return await _recorder.LoadAndBuildManyAsync(request.SuiteId, request.ConversationId, request.Model);
        }
        catch (InvalidOperationException ex)
        {
            // The segmenter rejects its own model's output rather than silently cutting cases in
            // the wrong place (see LlmCaseSegmenter.Parse). Surface that verbatim -- "the model
            // left turns 3..5 uncovered" tells the caller to just retry, which a 500 would not.
            return BadRequest($"AI extraction failed: {ex.Message}");
        }
    }

    /// <summary>
    /// [BotSharpAuth]: every trigger really calls the model and really spends token quota, with no
    /// usage throttling anywhere -- a cost-escalation surface just like RecordCase, so it is
    /// restricted to admin/root.
    /// </summary>
    [BotSharpAuth]
    [HttpPost("suites/{id}/run")]
    public async Task<ActionResult<AgentTestRun>> TriggerRun(
        string id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AgentTestRunTriggerRequest? request)
    {
        var suite = await _repo.GetSuiteAsync(id);
        if (suite == null)
        {
            return NotFound($"agent test suite {id} not found");
        }

        // Reject an unknown provider/model here rather than letting every case in the run die
        // deep inside model resolution. Measured: an unregistered model name surfaces as a bare
        // "Object reference not set to an instance of an object." on each case result, which tells
        // the author nothing about what they actually got wrong.
        if (ValidateRequestedModels(request?.Models) is { } modelError)
        {
            return BadRequest(modelError);
        }

        if (!suite.Enabled)
        {
            // The suite's own Enabled flag is advertised in the design as a way to turn a whole
            // suite off; AgentTestCase.Enabled is honoured by the executor, but nothing previously
            // checked the suite's own flag anywhere, so disabling a suite silently did nothing --
            // a caller (or a scheduled re-run) could still trigger it. Reject at the one place this
            // whole path runs inside a real HTTP request, before a run row is ever created.
            return BadRequest($"agent test suite {id} is disabled");
        }

        var run = new AgentTestRun
        {
            SuiteId = id,
            Status = AgentTestStatus.Pending,
            // Re-running only a caller-selected subset (e.g. "just the cases that failed last
            // time") is core value for a regression harness -- AgentTestRunExecutor filters the
            // suite's enabled cases down to this list when it's non-empty.
            CaseIds = request?.CaseIds,
            // Empty list and null mean the same thing downstream ("agent's own LlmConfig"), so
            // normalise to null rather than persisting an empty array that reads like a choice.
            Models = request?.Models is { Count: > 0 } ? request.Models : null,
            // This controller action is the only place in the whole trigger-to-execute path that
            // ever runs inside a real HTTP request -- AgentTestRunQueue's background loop has no
            // HttpContext at all, so if the triggering identity isn't captured here it can never
            // be recovered later. ClaimTypes.NameIdentifier matches what this codebase's own
            // ICurrentUser/CurrentUserIdentity.Id already reads as "who is this" elsewhere
            // (BusinessCore.Abstraction.CurrentUserIdentity), read directly off User here instead
            // of taking on that whole service as a new dependency for one claim.
            TriggeredBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        await _repo.CreateRunAsync(run);

        // Drop the runId on the queue and return immediately -- nothing waits for it here. The
        // actual execution happens in AgentTestRunQueue's background loop.
        _queue.Enqueue(run.Id);

        return run;
    }

    [HttpGet("runs")]
    public async Task<List<AgentTestRun>> ListRuns([FromQuery] string? suiteId)
        => await _repo.ListRunsAsync(suiteId);

    [HttpGet("runs/{id}")]
    public async Task<ActionResult<AgentTestRunDetailDto>> GetRun(string id)
    {
        var run = await _repo.GetRunAsync(id);
        if (run == null)
        {
            return NotFound($"agent test run {id} not found");
        }

        var results = await _repo.ListCaseResultsAsync(id);
        return new AgentTestRunDetailDto { Run = run, Results = results };
    }

    [HttpPost("runs/{id}/cancel")]
    public async Task<IActionResult> CancelRun(string id)
    {
        var run = await _repo.GetRunAsync(id);
        if (run == null)
        {
            return NotFound($"agent test run {id} not found");
        }

        if (IsTerminalStatus(run.Status))
        {
            // A run that already finished has nothing left to cancel; silently accepting this
            // (the old behaviour) makes a stale/duplicate cancel click on a finished run look
            // successful with no signal that it did nothing.
            return Conflict($"agent test run {id} has already finished ({run.Status}) and cannot be cancelled");
        }

        run.CancelRequested = true;
        await _repo.UpdateRunAsync(run);
        return Ok();
    }

    [HttpGet("mock-targets")]
    public async Task<ActionResult<List<string>>> GetMockTargets([FromQuery] string? agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return BadRequest("agentId is required");
        }

        var agent = await _agents.GetAgent(agentId);
        if (agent == null)
        {
            return NotFound($"agent {agentId} not found");
        }

        var names = new List<string>();
        names.AddRange((agent.Functions ?? []).Select(f => f.Name));
        names.AddRange((agent.SecondaryFunctions ?? []).Select(f => f.Name));
        names.AddRange((agent.McpTools ?? []).SelectMany(t => t.Functions ?? []).Select(f => f.Name));

        return names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Null when every requested model is registered (or none was requested); otherwise a
    /// caller-facing 400 message naming the first offender.
    /// </summary>
    private string? ValidateRequestedModels(List<TestModel>? models)
    {
        if (models is not { Count: > 0 })
        {
            return null;
        }

        foreach (var model in models)
        {
            if (string.IsNullOrWhiteSpace(model.Provider) || string.IsNullOrWhiteSpace(model.Model))
            {
                return "each entry in 'models' needs both a provider and a model";
            }

            if (_llmProviders.GetSetting(model.Provider, model.Model) == null)
            {
                return $"model '{model.Provider}/{model.Model}' is not registered; "
                    + "see GET /llm-configs for what this host can actually run";
            }
        }

        // Two identical entries would run the same case twice under the same label and make the
        // comparison grid collapse two results into one cell -- the second silently overwriting
        // the first.
        var duplicate = models
            .GroupBy(m => $"{m.Provider}/{m.Model}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        return duplicate == null ? null : $"model '{duplicate.Key}' is listed more than once";
    }

    private static void ApplySuite(AgentTestSuite suite, AgentTestSuiteUpsertRequest request)
    {
        suite.AgentId = request.AgentId;
        suite.Name = request.Name;
        suite.Description = request.Description;
        // request.Enabled is null when the request body omits "enabled" -- keep whatever the
        // suite already had (== the entity's own default of true, for a brand-new suite created
        // via CreateSuite's `new AgentTestSuite()`) rather than defaulting to false or, worse,
        // silently re-enabling a suite someone had deliberately disabled via a partial PUT that
        // only meant to change some other field.
        suite.Enabled = request.Enabled ?? suite.Enabled;
        suite.JudgeProvider = request.JudgeProvider;
        suite.JudgeModel = request.JudgeModel;
        suite.ExtraAllowedFunctions = request.ExtraAllowedFunctions ?? [];
        suite.ForceBlockedFunctions = request.ForceBlockedFunctions ?? [];
        suite.CaseTimeoutSeconds = request.CaseTimeoutSeconds;
    }

    private static void ApplyCase(AgentTestCase testCase, AgentTestCaseUpsertRequest request)
    {
        testCase.SuiteId = request.SuiteId;
        testCase.Name = request.Name;
        testCase.Enabled = request.Enabled;
        testCase.Turns = request.Turns ?? [];
        testCase.Assertions = request.Assertions ?? [];
        testCase.InitialStates = request.InitialStates ?? [];
        testCase.Mocks = request.Mocks ?? [];
        testCase.UnmockedToolPolicy = request.UnmockedToolPolicy;
        testCase.SourceConversationId = request.SourceConversationId;
    }

    /// <summary>
    /// Shared create/update validation for a case payload. Null means the payload is acceptable;
    /// otherwise the string is a caller-facing 400 message.
    /// </summary>
    private static string? ValidateCasePayload(AgentTestCaseUpsertRequest request)
    {
        if (IsUnsupportedUnmockedToolPolicy(request.UnmockedToolPolicy))
        {
            return "Passthrough is not supported in P1";
        }

        var allAssertions = (request.Turns ?? [])
            .SelectMany(t => t.Assertions ?? [])
            .Concat(request.Assertions ?? []);

        foreach (var assertion in allAssertions)
        {
            var error = AssertionValidation.Validate(assertion);
            if (error != null)
            {
                return error;
            }
        }

        return null;
    }

    /// <summary>
    /// Passthrough was specified in the design/plan and even had a (dead) code path, but nothing
    /// ever back-fills an ObservedToolCall for a tool the provider let run for real -- under it,
    /// toolNotCalled always vacuously passed against a tool that genuinely executed with real side
    /// effects. Rejected here rather than implementing the back-fill (project owner decision).
    /// </summary>
    private static bool IsUnsupportedUnmockedToolPolicy(string? policy)
        => string.Equals(policy, "Passthrough", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalStatus(string status) =>
        status is AgentTestStatus.Passed or AgentTestStatus.Failed
            or AgentTestStatus.Error or AgentTestStatus.Cancelled;
}
