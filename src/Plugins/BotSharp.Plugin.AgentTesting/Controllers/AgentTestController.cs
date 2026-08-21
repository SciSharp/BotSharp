using System.Security.Claims;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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

        if (await ValidateEntryAgentAsync(request) is { } entryAgentError)
        {
            return BadRequest(entryAgentError);
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

        if (await ValidateEntryAgentAsync(request) is { } entryAgentError)
        {
            return BadRequest(entryAgentError);
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

    /// <summary>
    /// Duplicates a case inside its own suite and returns the copy.
    ///
    /// Server-side rather than a client GET-then-POST because the copy has to carry EVERY field the
    /// case has. A client that rebuilds the payload from its own form drops whatever it does not know
    /// about, and a copy missing its mocks is indistinguishable in the list from a correct one --
    /// right up to the run where it blocks every tool the agent reaches for.
    ///
    /// Not cross-suite on purpose: moving a case between suites also changes which agent it runs
    /// against, so the copy would need a different entry agent and different mock targets to mean
    /// anything. That is an edit, not a copy.
    /// </summary>
    /// <summary>
    /// Which cases a change needs to run, and -- just as importantly -- which it does not, with the
    /// reason for each.
    ///
    /// Read-only and side-effect free: it plans a run, it does not start one. Triggering stays
    /// per-suite (POST suites/{id}/run), so a caller takes the included case ids from here and
    /// triggers each suite that appears among them.
    /// </summary>
    [HttpPost("scope")]
    public async Task<ActionResult<ScopeSelectionResponse>> SelectScope([FromBody] ScopeSelectionRequest request)
    {
        request ??= new ScopeSelectionRequest();

        if (request.Batch is { } batch && !CaseBatches.All.Contains(batch))
        {
            return BadRequest($"batch must be one of {string.Join(", ", CaseBatches.All)}, not {batch}");
        }

        var targets = (request.TargetAgentIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Naming no agents and not declaring a platform-wide change would narrow to nothing at all,
        // and an empty scope reported as a successful plan is the single most dangerous answer this
        // endpoint could give.
        if (targets.Count == 0 && !request.FullPlatform)
        {
            return BadRequest(
                "name at least one target agent, or set fullPlatform for a change with no single target");
        }

        var query = new ScopeQuery
        {
            TargetAgentIds = targets,
            FullPlatform = request.FullPlatform,
            Batch = request.Batch
        };

        var response = new ScopeSelectionResponse
        {
            TargetAgentIds = targets,
            FullPlatform = request.FullPlatform,
            Batch = request.Batch
        };

        foreach (var suite in await _repo.ListSuitesAsync(null))
        {
            foreach (var testCase in await _repo.ListCasesAsync(suite.Id))
            {
                var decision = CaseScope.Decide(testCase, suite.AgentId, query);
                var dto = new ScopedCaseDto
                {
                    CaseId = testCase.Id,
                    CaseName = testCase.Name,
                    SuiteId = suite.Id,
                    SuiteName = suite.Name,
                    CaseType = testCase.CaseType,
                    Priority = testCase.Priority,
                    Severity = testCase.Severity,
                    CrossCutting = testCase.CrossCutting,
                    Enabled = testCase.Enabled,
                    Batch = decision.Batch,
                    InvolvedAgentIds = decision.InvolvedAgentIds.ToList(),
                    Reason = decision.Reason
                };

                response.TotalCases++;
                (decision.Included ? response.Included : response.Excluded).Add(dto);
            }
        }

        return response;
    }

    [HttpPost("cases/{id}/copy")]
    public async Task<ActionResult<AgentTestCase>> CopyCase(string id)
    {
        var source = await _repo.GetCaseAsync(id);
        if (source == null)
        {
            return NotFound($"agent test case {id} not found");
        }

        // Cloned by a BSON round trip rather than field by field. A hand-written clone has exactly
        // the drift problem this endpoint exists to prevent: the next field added to AgentTestCase
        // would be silently absent from every copy, and nothing would fail until a run.
        var copy = BsonSerializer.Deserialize<AgentTestCase>(source.ToBsonDocument());

        // Blank so UpsertCaseAsync mints a new one -- the round trip copied the source's _id, and
        // keeping it would make the "copy" a full overwrite of the original.
        copy.Id = string.Empty;

        var siblings = await _repo.ListCasesAsync(source.SuiteId);
        copy.Name = NextCopyName(source.Name, siblings.Select(c => c.Name));

        // Disabled regardless of the source. An exact duplicate that joins the next run measures the
        // same thing twice: it pads the pass-rate denominator, and for a routing case it
        // double-weights one routing decision. A copy is made in order to be edited into a variant,
        // so it waits for that edit -- the same reason a recorded draft lands disabled.
        copy.Enabled = false;

        // The round trip also copied CreateDate, which would have the copy claim to be as old as the
        // case it came from. UpdateDate is set by the repository on write.
        copy.CreateDate = DateTime.UtcNow;

        await _repo.UpsertCaseAsync(copy);
        return copy;
    }

    /// <summary>
    /// "x" becomes "x (copy)", then "x (copy 2)", "x (copy 3)". Names are not unique in this store,
    /// so this is presentation rather than a constraint -- but two rows both called "x (copy)" are
    /// impossible to tell apart in the list, which is the one place copies are managed.
    ///
    /// Capped at the length the case editor's own input accepts, so the copy stays editable: a name
    /// the form cannot hold would have to be trimmed by hand before any other change could be saved.
    /// </summary>
    private static string NextCopyName(string name, IEnumerable<string> existing)
    {
        const int maxNameLength = 200;

        var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        for (var attempt = 1; ; attempt++)
        {
            var suffix = attempt == 1 ? " (copy)" : $" (copy {attempt})";
            var room = maxNameLength - suffix.Length;
            var stem = name.Length > room ? name[..room] : name;
            var candidate = stem + suffix;

            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    /// <summary>
    /// Clears run history: removes the named runs and the case results underneath them.
    ///
    /// Bulk-only, and the single-row button in the UI calls it with one id. A separate
    /// DELETE runs/{id} would be a second path to the same destructive operation, and the guard
    /// below is one of the things worth having in exactly one place.
    ///
    /// Behind the same admin gate as triggering a run. Runs are the audit trail for whether an agent
    /// change was evaluated at all, so removing them is at least as consequential as creating them.
    /// </summary>
    [BotSharpAuth]
    [HttpPost("runs/delete")]
    public async Task<ActionResult<AgentTestRunDeleteResponse>> DeleteRuns(
        [FromBody] AgentTestRunDeleteRequest request)
    {
        var runIds = (request?.RunIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (runIds.Count == 0)
        {
            return BadRequest("name at least one run to delete");
        }

        var response = new AgentTestRunDeleteResponse();

        foreach (var runId in runIds)
        {
            var run = await _repo.GetRunAsync(runId);
            if (run == null)
            {
                // Already gone. Reported rather than treated as an error: two people clearing the
                // same history is a normal race, not a failure worth refusing the whole batch over.
                response.Skipped.Add(new SkippedRunDto
                {
                    RunId = runId,
                    Reason = "already deleted"
                });
                continue;
            }

            if (!IsTerminalStatus(run.Status))
            {
                // Deleting a run that is still executing does not stop it: the queue keeps driving
                // cases, keeps spending tokens, and keeps writing results for a run id that no longer
                // exists -- results nothing can ever list again. Cancel first.
                response.Skipped.Add(new SkippedRunDto
                {
                    RunId = runId,
                    Reason = $"the run is {run.Status.ToLowerInvariant()}; cancel it before deleting"
                });
                continue;
            }

            response.DeletedResultCount += await _repo.DeleteRunAsync(runId);
            response.DeletedRunIds.Add(runId);
        }

        return response;
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
        // Normalised, not taken verbatim: ValidateCasePayload has already rejected anything that is
        // neither blank nor a known type, so this only fixes casing ("routing" -> "Routing") and
        // maps blank onto the default. Storing the caller's casing would break every later
        // Ordinal comparison against CaseTypes.Routing.
        testCase.CaseType = CaseTypes.Normalize(request.CaseType) ?? CaseTypes.Agent;
        testCase.EntryAgentId = string.IsNullOrWhiteSpace(request.EntryAgentId) ? null : request.EntryAgentId.Trim();
        testCase.Turns = request.Turns ?? [];
        testCase.Assertions = request.Assertions ?? [];
        testCase.InitialStates = request.InitialStates ?? [];
        testCase.History = (request.History ?? [])
            .Select(m => new TestHistoryMessage
            {
                // Normalised for the same reason CaseType is: the driver and every later comparison
                // use the canonical lowercase constants.
                Role = HistoryRoles.Normalize(m.Role) ?? HistoryRoles.User,
                Content = m.Content ?? string.Empty
            })
            .ToList();
        testCase.Mocks = request.Mocks ?? [];
        testCase.UnmockedToolPolicy = request.UnmockedToolPolicy;
        testCase.SourceConversationId = request.SourceConversationId;

        // Normalised, not verbatim: validation has already rejected anything unrecognised, so this
        // only fixes casing. Storing "p0" would leave a case that never matches an Ordinal comparison
        // against CasePriorities.P0 -- it would run, and then be filed in the wrong batch.
        testCase.Priority = CasePriorities.Normalize(request.Priority) ?? CasePriorities.P1;
        testCase.Severity = CaseSeverities.Normalize(request.Severity) ?? CaseSeverities.S1;
        testCase.Batch = request.Batch;
        testCase.CrossCutting = request.CrossCutting;
        testCase.InvolvedAgents = (request.InvolvedAgents ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        testCase.BusinessDomain = string.IsNullOrWhiteSpace(request.BusinessDomain)
            ? null
            : request.BusinessDomain.Trim();
        testCase.ExpectedOutcome = string.IsNullOrWhiteSpace(request.ExpectedOutcome)
            ? null
            : request.ExpectedOutcome.Trim();
        testCase.LastReviewedDate = request.LastReviewedDate;
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

        var caseType = CaseTypes.Normalize(request.CaseType);
        if (caseType == null && !string.IsNullOrWhiteSpace(request.CaseType))
        {
            return $"caseType must be one of {string.Join(", ", CaseTypes.All)}, not '{request.CaseType}'";
        }

        if (CasePriorities.Normalize(request.Priority) == null && !string.IsNullOrWhiteSpace(request.Priority))
        {
            return $"priority must be one of {string.Join(", ", CasePriorities.All)}, not '{request.Priority}'";
        }

        if (CaseSeverities.Normalize(request.Severity) == null && !string.IsNullOrWhiteSpace(request.Severity))
        {
            return $"severity must be one of {string.Join(", ", CaseSeverities.All)}, not '{request.Severity}'";
        }

        // Rejected rather than clamped: a batch of 4 is a mistake, and silently filing the case in
        // batch 3 would leave the author believing it runs somewhere it does not.
        if (request.Batch is { } batch && !CaseBatches.All.Contains(batch))
        {
            return $"batch must be one of {string.Join(", ", CaseBatches.All)}, not {batch}";
        }

        foreach (var (message, index) in (request.History ?? []).Select((m, i) => (m, i)))
        {
            if (HistoryRoles.Normalize(message?.Role) == null)
            {
                return $"history message {index + 1} has role '{message?.Role}'; only "
                     + $"{string.Join(" and ", HistoryRoles.All)} are supported";
            }

            // An empty message is dropped by BotSharp's own dialog storage (ConversationStorage
            // skips elements with blank content), so it would silently not be there at run time --
            // and the runner's count check would then fail the whole case with a confusing message
            // about the write having vanished.
            if (string.IsNullOrWhiteSpace(message!.Content))
            {
                return $"history message {index + 1} has no content";
            }
        }

        var allAssertions = (request.Turns ?? [])
            .SelectMany(t => t.Assertions ?? [])
            .Concat(request.Assertions ?? [])
            .ToList();

        foreach (var assertion in allAssertions)
        {
            var error = AssertionValidation.Validate(assertion);
            if (error != null)
            {
                return error;
            }
        }

        return ValidateRoutingCase(caseType ?? CaseTypes.Agent, request, allAssertions);
    }

    /// <summary>
    /// The extra rules a Routing case has to satisfy. A Routing case is not a label -- it is the only
    /// type counted towards a run's routing accuracy, so one that cannot actually establish a routing
    /// outcome would quietly move that figure without measuring anything.
    ///
    /// Enforced at save time for the same reason AssertionValidation is: the alternative is a case
    /// that saves cleanly and then reports a meaningless green every run.
    /// </summary>
    private static string? ValidateRoutingCase(
        string caseType, AgentTestCaseUpsertRequest request, List<TestAssertion> allAssertions)
    {
        if (!string.Equals(caseType, CaseTypes.Routing, StringComparison.Ordinal))
        {
            return null;
        }

        // Routing is a single-turn question: which agent picks this message up. A second turn is
        // either a different question (making it an Agent case) or an accident, and either way its
        // result would be counted as routing accuracy.
        //
        // Authored History is not a turn and is deliberately not counted here: replaying a prior
        // exchange and then asking one question is still a single routing decision, and it is the
        // most realistic way to test routing that depends on context.
        if ((request.Turns ?? []).Count != 1)
        {
            return "a Routing case must have exactly one turn; use an Agent case for a multi-turn case";
        }

        // Without one of these the case asserts nothing about routing, yet still counts towards
        // routing accuracy -- it would report Passed for having successfully said anything at all.
        var assertsRouting = allAssertions.Any(a =>
            string.Equals(a.Type, AssertionTypes.RoutedToAgent, StringComparison.Ordinal)
            || string.Equals(a.Type, AssertionTypes.AgentChain, StringComparison.Ordinal));
        if (!assertsRouting)
        {
            return $"a Routing case needs at least one '{AssertionTypes.RoutedToAgent}' or "
                 + $"'{AssertionTypes.AgentChain}' assertion, otherwise it verifies no routing outcome";
        }

        // The framework this implements scores routing purely as expected-agent == actual-agent and
        // deliberately applies no quality judgement to it. An llmJudge here would also make the
        // routing figure depend on a vendor call, so a vendor outage would read as a routing
        // regression.
        if (allAssertions.Any(a => string.Equals(a.Type, AssertionTypes.LlmJudge, StringComparison.Ordinal)))
        {
            return $"a Routing case cannot use '{AssertionTypes.LlmJudge}': routing is judged only by "
                 + "which agent handled the conversation";
        }

        return null;
    }

    /// <summary>
    /// Null when the request's entry agent is usable. Separate from
    /// <see cref="ValidateCasePayload"/> because it needs IAgentService, so it cannot be static, and
    /// a lookup is worth doing only once the cheap checks have passed.
    /// </summary>
    private async Task<string?> ValidateEntryAgentAsync(AgentTestCaseUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EntryAgentId))
        {
            return null;
        }

        var agent = await _agents.GetAgent(request.EntryAgentId.Trim());
        return agent == null ? $"entry agent {request.EntryAgentId} not found" : null;
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
