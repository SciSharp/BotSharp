using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.AgentTesting.Repositories;

namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// The model-backed <see cref="ICaseAuthor"/>. Contract and boundary live on the interface.
///
/// Extends the model's answer no trust, in four separate places, because each guards a different
/// failure: the whitelist merge stops a field being deleted by omission, the function-name check
/// stops a mock that could never match, the validation pass stops a draft that cannot be saved, and
/// the diff is computed rather than read off the model's own account of what it did.
/// </summary>
public class LlmCaseAuthor : ICaseAuthor
{
    /// <summary>
    /// How much of the authoring conversation is replayed. The draft carries the state, so older
    /// turns add tokens without adding information -- and every turn of this chat pays for the whole
    /// context block again.
    /// </summary>
    private const int MaxChatMessages = 20;

    private const int MaxInstructionChars = 4000;
    private const int MaxExistingCases = 25;
    private const int MaxGroundedTurns = 10;
    private const int MaxGroundedOutputChars = 600;
    private const int MaxGroundedToolCalls = 20;
    private const int MaxGroundedArgsChars = 300;

    /// <summary>
    /// How many recent runs are searched for a result belonging to the case being edited. Bounded
    /// because each one is a separate query, and a case that has not run in the last five runs of
    /// its own suite is one whose old output would be misleading grounding anyway.
    /// </summary>
    private const int MaxRunsScanned = 5;

    /// <summary>
    /// camelCase both ways: this is the shape the case editor already posts to /agent-test/cases, so
    /// a draft can travel from the model straight into the editor's form and back out to the save
    /// endpoint without a second naming convention in between.
    /// </summary>
    private static readonly JsonSerializerOptions DraftJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IServiceProvider _services;
    private readonly IAgentService _agents;
    private readonly IAgentTestRepository _repo;
    private readonly ILogger<LlmCaseAuthor> _logger;

    public LlmCaseAuthor(
        IServiceProvider services,
        IAgentService agents,
        IAgentTestRepository repo,
        ILogger<LlmCaseAuthor> logger)
    {
        _services = services;
        _agents = agents;
        _repo = repo;
        _logger = logger;
    }

    public async Task<AgentTestAuthorResponse> AuthorAsync(
        AgentTestSuite suite,
        AgentTestAuthorRequest request,
        CancellationToken ct)
    {
        var messages = (request.Messages ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m?.Content))
            .ToList();

        if (messages.Count == 0
            || !string.Equals(AuthorChatRoles.Normalize(messages[^1].Role), AuthorChatRoles.User, StringComparison.Ordinal))
        {
            throw new CaseAuthorUnavailableException(
                "the last message must be a user message saying what to do");
        }

        var model = ResolveModel(suite, request.Model);
        var completion = _services.GetServices<IChatCompletion>()
            .FirstOrDefault(x => string.Equals(x.Provider, model.Provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new CaseAuthorUnavailableException(
                $"no chat completion provider is registered for '{model.Provider}'");

        completion.SetModelName(model.Model);

        // The draft's own entry agent wins over the suite's, for the same reason the runner honours
        // it: a case pointed at a leaf agent is testing that agent, and authoring it against the
        // router's instruction and the router's functions would describe the wrong thing.
        var agentId = string.IsNullOrWhiteSpace(request.Draft?.EntryAgentId)
            ? suite.AgentId
            : request.Draft!.EntryAgentId!.Trim();

        var agent = await _agents.GetAgent(agentId)
            ?? throw new CaseAuthorUnavailableException(
                $"agent {agentId} not found, so there is nothing to author a case against");

        var utilityAssistant = await _agents.GetAgent(BuiltInAgentId.UtilityAssistant);
        var targets = MockTargetCatalogue.Describe(agent, utilityAssistant);
        var existingCases = await _repo.ListCasesAsync(suite.Id);
        var grounding = await LoadGroundingAsync(suite.Id, request.CaseId, ct);
        var agentNames = await AgentNamesAsync();

        // The baseline is what every merge and every diff is measured against. Cloned so a merge
        // cannot mutate the caller's object and leave the diff comparing a thing with itself.
        var baseline = Clone(request.Draft ?? new AgentTestCaseUpsertRequest());
        baseline.SuiteId = suite.Id;

        var instruction = BuildInstruction();
        var dialogs = new List<RoleDialogModel>
        {
            new(AgentRole.User, BuildContext(agent, targets, existingCases, request.CaseId, grounding, agentNames))
        };

        foreach (var message in messages.Take(messages.Count - 1).TakeLast(MaxChatMessages))
        {
            var role = string.Equals(AuthorChatRoles.Normalize(message.Role), AuthorChatRoles.Assistant, StringComparison.Ordinal)
                ? AgentRole.Assistant
                : AgentRole.User;

            dialogs.Add(new RoleDialogModel(role, message.Content));
        }

        // The draft goes in the last message rather than the context block: it is the thing that
        // changes every turn, and it is what the instruction tells the model to echo field names from.
        dialogs.Add(new RoleDialogModel(AgentRole.User, BuildTask(baseline, messages[^1].Content)));

        var attempt = await AskWithParseRepairAsync(completion, instruction, dialogs, model, ct);
        var response = Assemble(baseline, attempt, targets, existingCases, suite);

        if (response.ValidationErrors.Count == 0)
        {
            _logger.LogInformation(
                "Case author changed {ChangeCount} field(s) with {Model}, {WarningCount} warning(s).",
                response.Changes.Count, model, response.Warnings.Count);

            return response;
        }

        // One repair round against the real error text -- the same single-retry stance the segmenter
        // and the judge take. Merged from the baseline again, never from the rejected draft:
        // repairing on top of something already invalid compounds the mistake.
        _logger.LogInformation(
            "Case author draft was rejected ({Error}); asking {Model} to repair it once.",
            response.ValidationErrors[0], model);

        dialogs.Add(new RoleDialogModel(AgentRole.Assistant, attempt.Raw));
        dialogs.Add(new RoleDialogModel(AgentRole.User,
            $"""
            That draft was rejected by validation:
            {string.Join("\n", response.ValidationErrors)}

            Fix exactly that and answer with the same JSON envelope again.
            """));

        var repaired = await AskWithParseRepairAsync(completion, instruction, dialogs, model, ct);
        var second = Assemble(baseline, repaired, targets, existingCases, suite);

        if (second.ValidationErrors.Count == 0)
        {
            return second;
        }

        // Still invalid. The baseline comes back untouched and the errors are stated: an invalid
        // draft presented as progress would overwrite a working one in the editor.
        _logger.LogWarning(
            "Case author could not produce a valid draft after a repair round: {Error}",
            second.ValidationErrors[0]);

        return new AgentTestAuthorResponse
        {
            Reply = second.Reply,
            Draft = baseline,
            DraftChanged = false,
            Changes = [],
            ValidationErrors = second.ValidationErrors,
            Warnings = second.Warnings
        };
    }

    /// <summary>
    /// No silent default, for the reason LlmAgentTestJudge gives: BotSharp's own InstructService falls
    /// back to openai/gpt-4o, and inheriting that here would author cases with a model nobody chose.
    /// The suite's judge model is used as the fallback rather than a new setting, because a suite that
    /// has one has already had a model chosen for it deliberately.
    /// </summary>
    private static TestModel ResolveModel(AgentTestSuite suite, TestModel? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested?.Provider) && !string.IsNullOrWhiteSpace(requested?.Model))
        {
            return requested!;
        }

        if (!string.IsNullOrWhiteSpace(suite.JudgeProvider) && !string.IsNullOrWhiteSpace(suite.JudgeModel))
        {
            return new TestModel { Provider = suite.JudgeProvider!, Model = suite.JudgeModel! };
        }

        throw new CaseAuthorUnavailableException(
            "no model to author with: pass a provider and model, or set this suite's judgeProvider "
            + "and judgeModel");
    }

    /// <summary>
    /// Agent names, because routedToAgent and agentChain match by name or id -- and a model writing a
    /// name it invented produces an assertion that can never pass, which reads as a routing
    /// regression forever after.
    /// </summary>
    private async Task<List<string>> AgentNamesAsync()
    {
        var options = await _agents.GetAgentOptions();

        return (options ?? [])
            .Select(o => o.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The most recent result for the case being edited, if it has ever run. This is what lets the
    /// model propose an outputContains against text the agent really produced and an argsMatchJson
    /// against arguments it really passed, instead of inventing both.
    /// </summary>
    private async Task<AgentTestCaseResult?> LoadGroundingAsync(string suiteId, string? caseId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return null;
        }

        var runs = await _repo.ListRunsAsync(suiteId);   // newest first
        foreach (var run in runs.Take(MaxRunsScanned))
        {
            ct.ThrowIfCancellationRequested();

            var results = await _repo.ListCaseResultsAsync(run.Id);
            var hit = results.FirstOrDefault(r => string.Equals(r.CaseId, caseId, StringComparison.Ordinal));
            if (hit != null)
            {
                return hit;
            }
        }

        return null;
    }

    /// <summary>
    /// Calls the model and parses its reply, retrying once if the reply cannot be read at all --
    /// the same single-retry stance the validation-repair round in <see cref="AuthorAsync"/> takes,
    /// extended to cover the other way a model's answer can be unusable. <see cref="Parse"/> already
    /// repairs the commonest cause (a JSON-as-text field written as a nested object) deterministically
    /// before this is ever reached, so landing here needs a genuinely different mistake -- truncated
    /// output, a stray comma, prose with no JSON at all.
    /// </summary>
    private async Task<AuthorAttempt> AskWithParseRepairAsync(
        IChatCompletion completion,
        string instruction,
        List<RoleDialogModel> dialogs,
        TestModel model,
        CancellationToken ct)
    {
        var (raw, attempt, error) = await AskOnceAsync(completion, instruction, dialogs, ct);
        if (attempt != null)
        {
            return attempt;
        }

        _logger.LogInformation(
            "Case author reply could not be read ({Error}); asking {Model} to resend it.", error, model);

        dialogs.Add(new RoleDialogModel(AgentRole.Assistant, raw));
        dialogs.Add(new RoleDialogModel(AgentRole.User,
            $"""
            That reply could not be read: {error}

            Resend the full JSON envelope, valid this time. Remember: argsMatchJson, resultContent and
            any state "value" must be a JSON string (escaped), never a nested object or array.
            """));

        var (_, repaired, repairError) = await AskOnceAsync(completion, instruction, dialogs, ct);
        return repaired ?? throw new CaseAuthorUnavailableException(
            $"the authoring model did not return a usable reply after a retry: {repairError}");
    }

    /// <summary>
    /// One model call, parsed but never throwing on a parse failure -- the caller decides whether
    /// that is retryable. A genuine vendor failure (timeout, rate limit, bad key) still throws
    /// immediately: asking the same vendor again in the same turn cannot fix that, and treating it as
    /// retryable would waste a call and hide the real error behind a generic "no usable reply".
    /// </summary>
    private async Task<(string Raw, AuthorAttempt? Attempt, string? Error)> AskOnceAsync(
        IChatCompletion completion,
        string instruction,
        List<RoleDialogModel> dialogs,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var promptAgent = new Agent
        {
            Id = Guid.Empty.ToString(),
            Name = "AgentTestCaseAuthor",
            Instruction = instruction
        };

        string raw;
        try
        {
            var response = await completion.GetChatCompletions(promptAgent, dialogs);
            raw = response?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            // A vendor timeout, a rate limit, a bad key. None of these produced a draft, and none of
            // them should look like one.
            throw new CaseAuthorUnavailableException($"the authoring model call failed: {ex.Message}", ex);
        }

        try
        {
            return (raw, Parse(raw), null);
        }
        catch (CaseAuthorUnavailableException ex)
        {
            return (raw, null, ex.Message);
        }
    }

    /// <summary>
    /// Reads the envelope. Public and static so the parsing rules can be tested without a vendor.
    /// </summary>
    public static AuthorAttempt Parse(string raw)
    {
        var json = ExtractJson(raw)
            ?? throw new CaseAuthorUnavailableException(
                $"the authoring model did not return JSON. First 200 chars: {Truncate(raw, 200)}");

        json = CoerceStructuredJsonStringFields(json);

        AuthorEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<AuthorEnvelope>(json, DraftJson);
        }
        catch (JsonException ex)
        {
            throw new CaseAuthorUnavailableException($"the authoring model returned malformed JSON: {ex.Message}");
        }

        if (envelope == null)
        {
            throw new CaseAuthorUnavailableException("the authoring model returned an empty result");
        }

        var declared = (envelope.ChangedFields ?? [])
            .Select(AuthorFields.Normalize)
            .Where(f => f != null)
            .Select(f => f!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // An unwritable field name does not fail the turn -- the merge would ignore it anyway -- but
        // it is reported, because "I renamed the suite for you" needs to be visibly untrue.
        var rejected = (envelope.ChangedFields ?? [])
            .Where(f => !string.IsNullOrWhiteSpace(f) && AuthorFields.Normalize(f) == null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthorAttempt(envelope.Reply ?? string.Empty, declared, rejected, envelope.Draft, raw);
    }

    /// <summary>
    /// Merge, sanitise, validate, diff. Deterministic given the same attempt, catalogue and suite.
    ///
    /// Public and static for the reason <see cref="Parse"/> and LlmAgentTestJudge.ParseVerdict are:
    /// everything that decides what a model is allowed to do to a draft happens here, and it should
    /// be testable without a vendor call.
    /// </summary>
    public static AgentTestAuthorResponse Assemble(
        AgentTestCaseUpsertRequest baseline,
        AuthorAttempt attempt,
        List<MockTargetInfo> targets,
        List<AgentTestCase> existingCases,
        AgentTestSuite suite)
    {
        var warnings = new List<string>();

        foreach (var field in attempt.RejectedFields)
        {
            warnings.Add($"the model asked to change '{field}', which is not a field it may change; ignored");
        }

        var merged = Clone(baseline);

        if (attempt.DeclaredFields.Count > 0)
        {
            if (attempt.Draft == null)
            {
                warnings.Add("the model said it changed the draft but returned no draft; nothing was changed");
            }
            else
            {
                foreach (var field in attempt.DeclaredFields)
                {
                    Apply(merged, attempt.Draft, field);
                }
            }
        }

        // Never from the model: the suite id comes from the request, and only Block is supported.
        merged.SuiteId = baseline.SuiteId;
        merged.UnmockedToolPolicy = UnmockedToolPolicies.Block;

        Sanitise(merged, targets, existingCases, suite, warnings);

        var validationError = CaseValidation.Validate(merged);
        var changes = Diff(baseline, merged);

        return new AgentTestAuthorResponse
        {
            Reply = attempt.Reply,
            Draft = merged,
            DraftChanged = changes.Count > 0,
            Changes = changes,
            ValidationErrors = validationError == null ? [] : [validationError],
            Warnings = warnings
        };
    }

    /// <summary>
    /// Copies one declared field off the model's draft. An explicit switch rather than reflection:
    /// which fields a model may write is a decision worth being able to read in one place, not one
    /// that should emerge from property metadata and change whenever the DTO gains a field.
    /// </summary>
    private static void Apply(AgentTestCaseUpsertRequest target, AgentTestCaseUpsertRequest source, string field)
    {
        switch (field)
        {
            case AuthorFields.Name: target.Name = source.Name ?? string.Empty; break;
            case AuthorFields.Enabled: target.Enabled = source.Enabled; break;
            case AuthorFields.CaseType: target.CaseType = source.CaseType; break;
            case AuthorFields.EntryAgentId: target.EntryAgentId = source.EntryAgentId; break;
            case AuthorFields.Turns: target.Turns = source.Turns ?? []; break;
            case AuthorFields.Assertions: target.Assertions = source.Assertions ?? []; break;
            case AuthorFields.InitialStates: target.InitialStates = source.InitialStates ?? []; break;
            case AuthorFields.History: target.History = source.History ?? []; break;
            case AuthorFields.Mocks: target.Mocks = source.Mocks ?? []; break;
            case AuthorFields.Priority: target.Priority = source.Priority; break;
            case AuthorFields.Severity: target.Severity = source.Severity; break;
            case AuthorFields.Batch: target.Batch = source.Batch; break;
            case AuthorFields.CrossCutting: target.CrossCutting = source.CrossCutting; break;
            case AuthorFields.InvolvedAgents: target.InvolvedAgents = source.InvolvedAgents ?? []; break;
            case AuthorFields.BusinessDomain: target.BusinessDomain = source.BusinessDomain; break;
            case AuthorFields.ExpectedOutcome: target.ExpectedOutcome = source.ExpectedOutcome; break;
        }
    }

    /// <summary>
    /// Corrects what is provably wrong and flags what is merely suspect.
    ///
    /// The split matters. A function name is authoritative -- it comes from the agent definition, so a
    /// mock naming something else can never match and is dropped. A state key is not: nothing in this
    /// system enumerates the keys an agent writes (a state value records the message it was written
    /// on and a coarse source, never a function name), so the only key list available is the one
    /// other cases in this suite happen to use. Dropping a key for being absent from an admittedly
    /// incomplete list would delete correct work, so an unknown key is a warning and stays.
    /// </summary>
    private static void Sanitise(
        AgentTestCaseUpsertRequest draft,
        List<MockTargetInfo> targets,
        List<AgentTestCase> existingCases,
        AgentTestSuite suite,
        List<string> warnings)
    {
        var callable = new HashSet<string>(targets.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);

        var keptMocks = new List<TestToolMock>();
        foreach (var mock in draft.Mocks ?? [])
        {
            if (string.IsNullOrWhiteSpace(mock.FunctionName) || !callable.Contains(mock.FunctionName))
            {
                warnings.Add($"dropped a mock for '{mock.FunctionName}': this agent has no such function, "
                           + "so the mock could never match at run time");
                continue;
            }

            keptMocks.Add(mock);
        }
        draft.Mocks = keptMocks;

        var turns = new List<TestTurn>();
        foreach (var (turn, index) in (draft.Turns ?? []).Select((t, i) => (t, i)))
        {
            // Re-indexed here as well as in the editor: the runner reads turns in order, and a model
            // that renumbered them while inserting one would silently reorder the case.
            turn.Index = index;
            turn.Assertions = FilterAssertions(turn.Assertions, callable, $"turn {index + 1}", warnings);
            turns.Add(turn);
        }
        draft.Turns = turns;

        draft.Assertions = FilterAssertions(draft.Assertions, callable, "the case", warnings);

        var knownStateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in StateKeysOf(existingCases))
        {
            knownStateKeys.Add(key);
        }

        var allAssertions = (draft.Assertions ?? [])
            .Concat((draft.Turns ?? []).SelectMany(t => t.Assertions ?? []))
            .ToList();

        var assertedStateKeys = allAssertions
            .Where(a => string.Equals(a.Type, AssertionTypes.StateEquals, StringComparison.Ordinal))
            .Select(a => a.Target)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var key in assertedStateKeys)
        {
            if (!knownStateKeys.Contains(key))
            {
                warnings.Add($"state key '{key}' is not used by any other case in this suite; "
                           + "check it is the key the agent really writes");
            }
        }

        var usesJudge = allAssertions.Any(a => string.Equals(a.Type, AssertionTypes.LlmJudge, StringComparison.Ordinal));
        if (usesJudge
            && (string.IsNullOrWhiteSpace(suite.JudgeProvider) || string.IsNullOrWhiteSpace(suite.JudgeModel)))
        {
            warnings.Add("this draft uses llmJudge but the suite has no judge model configured, so that "
                       + "assertion will fail rather than pass when the case runs");
        }
    }

    /// <summary>
    /// Every conversation state key any case in the suite touches -- injected, written by a mock, or
    /// asserted on. The nearest thing to a state key catalogue this system has; see
    /// <see cref="Sanitise"/> for why it is treated as incomplete.
    /// </summary>
    private static IEnumerable<string> StateKeysOf(List<AgentTestCase> cases)
        => cases
            .SelectMany(c => c.InitialStates.Select(s => s.Key)
                .Concat(c.Mocks.SelectMany(m => m.StateWrites ?? []).Select(s => s.Key))
                .Concat(c.Assertions.Concat(c.Turns.SelectMany(t => t.Assertions))
                    .Where(a => string.Equals(a.Type, AssertionTypes.StateEquals, StringComparison.Ordinal))
                    .Select(a => a.Target ?? string.Empty)))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

    private static List<TestAssertion> FilterAssertions(
        List<TestAssertion>? assertions,
        HashSet<string> callable,
        string where,
        List<string> warnings)
    {
        var kept = new List<TestAssertion>();

        foreach (var assertion in assertions ?? [])
        {
            var isToolAssertion =
                string.Equals(assertion.Type, AssertionTypes.ToolCalled, StringComparison.Ordinal)
                || string.Equals(assertion.Type, AssertionTypes.ToolNotCalled, StringComparison.Ordinal);

            if (isToolAssertion && !string.IsNullOrWhiteSpace(assertion.Target) && !callable.Contains(assertion.Target!))
            {
                warnings.Add($"dropped a {assertion.Type} assertion on {where}: this agent has no function "
                           + $"called '{assertion.Target}'");
                continue;
            }

            kept.Add(assertion);
        }

        return kept;
    }

    /// <summary>
    /// What actually changed, by comparing the two drafts field by field. Only whitelisted fields are
    /// compared, because the merge cannot have moved anything else.
    /// </summary>
    private static List<AuthorChange> Diff(AgentTestCaseUpsertRequest before, AgentTestCaseUpsertRequest after)
    {
        var changes = new List<AuthorChange>();

        foreach (var field in AuthorFields.All)
        {
            var oldValue = Read(before, field);
            var newValue = Read(after, field);

            if (string.Equals(Json(oldValue), Json(newValue), StringComparison.Ordinal))
            {
                continue;
            }

            changes.Add(new AuthorChange { Field = field, Detail = Describe(oldValue, newValue) });
        }

        return changes;
    }

    private static object? Read(AgentTestCaseUpsertRequest draft, string field) => field switch
    {
        AuthorFields.Name => draft.Name,
        AuthorFields.Enabled => draft.Enabled,
        AuthorFields.CaseType => draft.CaseType,
        AuthorFields.EntryAgentId => draft.EntryAgentId,
        AuthorFields.Turns => draft.Turns,
        AuthorFields.Assertions => draft.Assertions,
        AuthorFields.InitialStates => draft.InitialStates,
        AuthorFields.History => draft.History,
        AuthorFields.Mocks => draft.Mocks,
        AuthorFields.Priority => draft.Priority,
        AuthorFields.Severity => draft.Severity,
        AuthorFields.Batch => draft.Batch,
        AuthorFields.CrossCutting => draft.CrossCutting,
        AuthorFields.InvolvedAgents => draft.InvolvedAgents,
        AuthorFields.BusinessDomain => draft.BusinessDomain,
        AuthorFields.ExpectedOutcome => draft.ExpectedOutcome,
        _ => null
    };

    private static string Describe(object? before, object? after)
    {
        if (before is System.Collections.ICollection oldList && after is System.Collections.ICollection newList)
        {
            return oldList.Count == newList.Count
                ? $"{newList.Count} item(s), edited"
                : $"{oldList.Count} -> {newList.Count} item(s)";
        }

        return $"{Truncate(Text(before), 60)} -> {Truncate(Text(after), 60)}";
    }

    private static string Text(object? value)
        => value is null || (value is string s && string.IsNullOrWhiteSpace(s))
            ? "(empty)"
            : value.ToString() ?? "(empty)";

    private static string Json(object? value) => JsonSerializer.Serialize(value, DraftJson);

    private static AgentTestCaseUpsertRequest Clone(AgentTestCaseUpsertRequest source)
        => JsonSerializer.Deserialize<AgentTestCaseUpsertRequest>(Json(source), DraftJson)
           ?? new AgentTestCaseUpsertRequest();

    private static string? ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    /// <summary>
    /// Fixes the one JSON-shape mistake models reliably make against this schema: writing a
    /// "JSON as text" field -- an assertion or mock's argsMatchJson, a mock's resultContent, or a
    /// state's value -- as a nested object or array instead of a string holding escaped JSON. Both
    /// forms are syntactically valid JSON (which is why <see cref="ExtractJson"/> cannot catch this),
    /// they only disagree on the CLR type the strongly-typed field expects, and re-serialising the
    /// nested value back to text is the one lossless, unambiguous reading of what the model meant --
    /// unlike LlmAgentTestJudge.ParseVerdict's out-of-range score, there is no second plausible
    /// interpretation here to guard against by rejecting instead of coercing.
    ///
    /// Silent about anything it does not recognise: a genuine JSON syntax error is left for the real
    /// deserializer in <see cref="Parse"/> to report, with the exact path and reason a retry needs.
    /// </summary>
    private static string CoerceStructuredJsonStringFields(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return json;
        }

        if (root is not JsonObject envelope || FindObject(envelope, "draft") is not { } draft)
        {
            return json;
        }

        foreach (var assertion in FindArrayOfObjects(draft, "assertions"))
        {
            StringifyIfStructured(assertion, "argsMatchJson");
        }

        foreach (var turn in FindArrayOfObjects(draft, "turns"))
        {
            foreach (var assertion in FindArrayOfObjects(turn, "assertions"))
            {
                StringifyIfStructured(assertion, "argsMatchJson");
            }
        }

        foreach (var mock in FindArrayOfObjects(draft, "mocks"))
        {
            StringifyIfStructured(mock, "argsMatchJson");
            StringifyIfStructured(mock, "resultContent");

            foreach (var write in FindArrayOfObjects(mock, "stateWrites"))
            {
                StringifyIfStructured(write, "value");
            }
        }

        foreach (var state in FindArrayOfObjects(draft, "initialStates"))
        {
            StringifyIfStructured(state, "value");
        }

        return envelope.ToJsonString();
    }

    private static JsonObject? FindObject(JsonObject obj, string name)
        => FindProperty(obj, name).Value as JsonObject;

    private static IEnumerable<JsonObject> FindArrayOfObjects(JsonObject? obj, string name)
    {
        if (obj != null && FindProperty(obj, name).Value is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is JsonObject o)
                {
                    yield return o;
                }
            }
        }
    }

    /// <summary>
    /// Case-insensitive lookup: this runs before <see cref="DraftJson"/>'s
    /// PropertyNameCaseInsensitive gets a say, and a model that capitalises a field unexpectedly
    /// should not skip normalisation just because the casing assumed here did not match verbatim.
    /// </summary>
    private static KeyValuePair<string, JsonNode?> FindProperty(JsonObject obj, string name)
        => obj.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase));

    private static void StringifyIfStructured(JsonObject obj, string propertyName)
    {
        var found = FindProperty(obj, propertyName);
        if (found.Key != null && found.Value is JsonObject or JsonArray)
        {
            obj[found.Key] = JsonValue.Create(found.Value!.ToJsonString());
        }
    }

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max].TrimEnd() + "...";

    /// <summary>
    /// The rules. The assertion vocabulary is generated from
    /// <see cref="AssertionValidation.Authorable"/> rather than written out here, so a new assertion
    /// type cannot exist in validation and be invisible to the author.
    /// </summary>
    private static string BuildInstruction()
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            """
            You help a QA engineer write and edit ONE regression test case for a customer-service AI
            agent, by conversation. You do not run tests and you do not save anything: you propose a
            draft, and the human saves it.

            A case is: some turns of user messages, mocked tool returns so that nothing real happens,
            and assertions that decide pass or fail.

            HOW TO ANSWER
            - Reply in the language the user is writing in.
            - `reply` is what you say to them: what you changed and why, or a question when their
              request is genuinely ambiguous. Asking is better than guessing at a business rule.
            - `changedFields` lists ONLY the top-level fields you are changing this turn. Fields you
              do not list are kept exactly as they are, so listing a field you did not mean to touch
              is how someone's work gets lost.
            - When you are only answering a question or asking one, return an empty `changedFields`.
            - Return the FULL new value of every field you list, never a fragment. To add one turn,
              return all the turns including the new one.

            WHAT MAKES A GOOD ASSERTION
            - Prefer assertions about what the agent DID: toolCalled, stateEquals, routedToAgent,
              agentChain. Those survive a reworded reply.
            - Avoid outputContains unless the user names the exact text that matters (an id format, a
              required disclosure). Never write one from wording you imagined the agent would use --
              it fails the first time the model phrases things differently.
            - For "is the reply any good" requirements use llmJudge, with the criterion in `expected`.
            - Only use function names from CALLABLE FUNCTIONS. Never invent one.
            - Only use state keys from KNOWN STATE KEYS, or ones the user gave you. If you need a key
              you do not have, ask for it.
            - Every tool the case will trigger needs a mock, or the run blocks the call and the case
              fails. When a mocked function passes data to later turns through conversation state,
              put that in the mock's stateWrites.

            FIELDS THAT HOLD JSON AS TEXT
            argsMatchJson (on an assertion or a mock), a mock's resultContent, and any state "value"
            are JSON STRINGS, not nested objects: their value must be the escaped JSON text, exactly
            like this --
              "argsMatchJson": "{\"work_order_id\":\"12345\"}"
            NOT this --
              "argsMatchJson": {"work_order_id": "12345"}
            The second form is invalid for that field even though it is valid JSON overall.

            ASSERTION TYPES
            """);

        foreach (var type in AssertionValidation.Authorable)
        {
            var required = AssertionValidation.RequiredFieldName(type);
            var purpose = type switch
            {
                AssertionTypes.OutputContains => "the reply contains this text",
                AssertionTypes.OutputNotContains => "the reply does not contain this text",
                AssertionTypes.OutputRegex => "the reply matches this regular expression",
                AssertionTypes.ToolCalled => "this function was called; the optional argsMatchJson is an argument subset",
                AssertionTypes.ToolNotCalled => "this function was not called",
                AssertionTypes.StateEquals => "conversation state at this key equals expected",
                AssertionTypes.RoutedToAgent => "the agent named in expected handled the conversation",
                AssertionTypes.AgentChain => "expected is a comma-separated agent list, target is contains|ordered|exact",
                AssertionTypes.LlmJudge => "a model scores the reply 1-5 against the criterion in expected; minScore is the bar, 4 by default",
                _ => "see the case editor"
            };

            builder.Append("- ").Append(type);
            if (required != null)
            {
                builder.Append(" (requires `").Append(required).Append("`)");
            }
            builder.Append(": ").AppendLine(purpose);
        }

        builder.AppendLine(
            """

            CASE TYPE RULES
            - caseType "Routing" means "did the router pick the right agent": exactly one turn, at
              least one routedToAgent or agentChain assertion, and no llmJudge.
            - caseType "Agent" is everything else, including multi-agent journeys.
            - priority P0/P1/P2 decides which batch runs first; severity S0/S1/S2 says what a failure
              means. Leave them at P1/S1 unless the user says otherwise.

            OUTPUT
            Reply with JSON only. No prose outside it and no code fence:
            {"reply":"...","changedFields":["turns"],"draft":{ the full case draft }}

            The draft uses exactly the field names shown in CURRENT DRAFT.
            """);

        return builder.ToString();
    }

    private static string BuildContext(
        Agent agent,
        List<MockTargetInfo> targets,
        List<AgentTestCase> existingCases,
        string? caseId,
        AgentTestCaseResult? grounding,
        List<string> agentNames)
    {
        var builder = new StringBuilder();

        builder.AppendLine("AGENT UNDER TEST").AppendLine($"name: {agent.Name}");

        if (!string.IsNullOrWhiteSpace(agent.Description))
        {
            builder.AppendLine($"description: {agent.Description}");
        }

        if (!string.IsNullOrWhiteSpace(agent.Instruction))
        {
            // The most useful thing here -- it is where the business rules and the required slots
            // live -- and also the longest, so it is capped rather than left to crowd out the
            // function catalogue and the existing cases.
            builder.AppendLine("instruction:").AppendLine(Truncate(agent.Instruction, MaxInstructionChars));
        }

        builder.AppendLine().AppendLine("CALLABLE FUNCTIONS (mock and assert against these names only)");
        if (targets.Count == 0)
        {
            builder.AppendLine("(none -- this agent calls no tools, so the case needs no mocks)");
        }

        foreach (var target in targets)
        {
            builder.Append("- ").Append(target.Name);
            if (!string.IsNullOrWhiteSpace(target.Parameters))
            {
                builder.Append(" [").Append(target.Parameters).Append(']');
            }
            if (!string.IsNullOrWhiteSpace(target.Description))
            {
                builder.Append(": ").Append(Truncate(target.Description!, 200));
            }
            builder.AppendLine();
        }

        if (agentNames.Count > 0)
        {
            builder.AppendLine().AppendLine("AGENTS THAT EXIST (for routedToAgent and agentChain)")
                   .AppendLine(string.Join(", ", agentNames));
        }

        var stateKeys = StateKeysOf(existingCases).ToList();
        builder.AppendLine().AppendLine("KNOWN STATE KEYS (from other cases in this suite -- not a complete list)")
               .AppendLine(stateKeys.Count == 0 ? "(none yet)" : string.Join(", ", stateKeys));

        var others = existingCases
            .Where(c => !string.Equals(c.Id, caseId, StringComparison.Ordinal))
            .Take(MaxExistingCases)
            .ToList();

        if (others.Count > 0)
        {
            // Names and shapes only, never the bodies: this exists so the model does not write a
            // duplicate, and a full dump of every case would cost more than the whole rest of the
            // prompt.
            builder.AppendLine().AppendLine("CASES ALREADY IN THIS SUITE (do not duplicate them)");
            foreach (var other in others)
            {
                var assertionTypes = string.Join("/", other.Assertions
                    .Concat(other.Turns.SelectMany(t => t.Assertions))
                    .Select(a => a.Type)
                    .Distinct(StringComparer.Ordinal));

                builder.Append("- ").Append(other.Name)
                       .Append(" [").Append(other.CaseType).Append(", ").Append(other.Turns.Count).Append(" turn(s)");

                if (!string.IsNullOrWhiteSpace(assertionTypes))
                {
                    builder.Append(", ").Append(assertionTypes);
                }

                builder.AppendLine("]");
            }
        }

        if (grounding != null)
        {
            builder.AppendLine().AppendLine(
                $"WHAT HAPPENED LAST TIME THIS CASE RAN (status {grounding.Status}) -- real replies and real "
                + "tool arguments. Base any outputContains or argsMatchJson on THIS, not on invention.");

            foreach (var turn in grounding.Turns.Take(MaxGroundedTurns))
            {
                builder.Append("- turn ").Append(turn.Index).Append(" user: ").AppendLine(Truncate(turn.UserMessage, 200));

                if (!string.IsNullOrWhiteSpace(turn.Output))
                {
                    builder.Append("  agent said: ").AppendLine(Truncate(turn.Output!, MaxGroundedOutputChars));
                }

                var failed = string.Join("; ", turn.Assertions
                    .Where(a => !a.Passed)
                    .Select(a => $"{a.Type} ({a.Message})"));

                if (!string.IsNullOrWhiteSpace(failed))
                {
                    builder.Append("  failed: ").AppendLine(Truncate(failed, 300));
                }
            }

            foreach (var call in grounding.ObservedToolCalls.Take(MaxGroundedToolCalls))
            {
                builder.Append("- turn ").Append(call.TurnIndex).Append(" called ").Append(call.FunctionName)
                       .Append(" [").Append(call.Outcome).Append(']');

                if (!string.IsNullOrWhiteSpace(call.ArgsJson))
                {
                    builder.Append(" args: ").Append(Truncate(call.ArgsJson!, MaxGroundedArgsChars));
                }

                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string BuildTask(AgentTestCaseUpsertRequest draft, string instruction)
    {
        var builder = new StringBuilder();

        builder.AppendLine("CURRENT DRAFT").AppendLine(JsonSerializer.Serialize(draft, DraftJson)).AppendLine();
        builder.AppendLine("WHAT TO DO").AppendLine(instruction).AppendLine();

        // The format is restated here, not only in the instruction: the replayed assistant turns of
        // this chat are plain prose (only the `reply` field is kept client-side), and without a
        // reminder next to the actual request a model will follow that example and answer in prose.
        builder.AppendLine(
            """
            Answer with the JSON envelope only:
            {"reply":"...","changedFields":[...],"draft":{...}}
            """);

        return builder.ToString();
    }
}

/// <summary>One parsed model answer, before it is merged or trusted.</summary>
/// <param name="Reply">What to show the user.</param>
/// <param name="DeclaredFields">Recognised field names from changedFields.</param>
/// <param name="RejectedFields">changedFields entries naming no writable field.</param>
/// <param name="Draft">The model's draft; only declared fields are ever read from it.</param>
/// <param name="Raw">The raw reply, replayed to the model when asking it to repair a rejection.</param>
public record AuthorAttempt(
    string Reply,
    List<string> DeclaredFields,
    List<string> RejectedFields,
    AgentTestCaseUpsertRequest? Draft,
    string Raw);

/// <summary>Wire shape of the model's answer.</summary>
internal class AuthorEnvelope
{
    public string? Reply { get; set; }
    public List<string>? ChangedFields { get; set; }
    public AgentTestCaseUpsertRequest? Draft { get; set; }
}
