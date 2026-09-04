namespace BotSharp.Plugin.AgentTesting.Models;

/// <summary>
/// Body of POST /agent-test/author -- one turn of authoring a case by conversation.
///
/// Deliberately carries the whole conversation and the whole current draft, because the server keeps
/// no authoring session: there is no fifth collection, no draft lifecycle, no lock, and nothing to
/// clean up when someone closes the tab mid-sentence. The editor page already holds the draft as its
/// form state, so the draft it sends is exactly what the user is looking at, and what comes back
/// replaces it.
/// </summary>
public class AgentTestAuthorRequest
{
    /// <summary>Required: supplies the agent under test, and the judge model this falls back to.</summary>
    public string SuiteId { get; set; } = string.Empty;

    /// <summary>
    /// The case being edited, when there is one. Null while creating.
    ///
    /// Used only to ground the model: with a case id its most recent run result can be read, which
    /// is what turns "add an assertion" from invention into a proposal based on what the agent
    /// actually said and actually passed to its tools.
    /// </summary>
    public string? CaseId { get; set; }

    /// <summary>The authoring conversation so far, oldest first. The last entry is the new instruction.</summary>
    public List<AuthorChatMessage> Messages { get; set; } = [];

    /// <summary>
    /// The draft as it stands. Null means start from nothing, which is the first turn of creating a
    /// case.
    /// </summary>
    public AgentTestCaseUpsertRequest? Draft { get; set; }

    /// <summary>
    /// Optional model override. Null falls back to the suite's judge model, and if that is unset the
    /// request is refused rather than defaulting to a model nobody chose -- the same stance
    /// LlmAgentTestJudge takes.
    /// </summary>
    public TestModel? Model { get; set; }
}

/// <summary>One message in the authoring conversation. Only user and assistant: see <see cref="AuthorChatRoles"/>.</summary>
public class AuthorChatMessage
{
    public string Role { get; set; } = AuthorChatRoles.User;
    public string Content { get; set; } = string.Empty;
}

public static class AuthorChatRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";

    public static readonly string[] All = [User, Assistant];

    public static string? Normalize(string? value)
        => All.FirstOrDefault(r => string.Equals(r, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Result of one authoring turn: what to say to the user, and what the draft now is.
///
/// The draft is never saved by this endpoint. Persistence stays on POST/PUT /agent-test/cases, which
/// is the only path that runs the full validation including the entry-agent lookup -- and which the
/// human presses deliberately. An authoring model that could write to the case store would be one
/// misread instruction away from editing a case nobody asked it to touch.
/// </summary>
public class AgentTestAuthorResponse
{
    /// <summary>What the model says about what it did, shown in the chat panel.</summary>
    public string Reply { get; set; } = string.Empty;

    /// <summary>
    /// The draft after the merge. Always populated, even when nothing changed, so the client can
    /// assign it unconditionally.
    /// </summary>
    public AgentTestCaseUpsertRequest Draft { get; set; } = new();

    /// <summary>True when <see cref="Draft"/> differs from the one that was sent in.</summary>
    public bool DraftChanged { get; set; }

    /// <summary>
    /// Field-level diff, computed by comparing the incoming draft with the merged one -- never taken
    /// from the model's own account of what it did. A model that says it added one assertion while
    /// actually rewriting every turn has to be caught by something that does not ask it.
    /// </summary>
    public List<AuthorChange> Changes { get; set; } = [];

    /// <summary>
    /// What <see cref="BotSharp.Plugin.AgentTesting.Services.CaseValidation"/> says about the merged
    /// draft. Non-empty means the draft cannot be saved as it stands, and the client shows it as
    /// such rather than letting the user discover it on the save button.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = [];

    /// <summary>
    /// Things that were silently wrong and got corrected, or are suspect and were left alone: a mock
    /// dropped for naming a function this agent cannot call, a state key nothing else in the suite
    /// has ever used, an llmJudge assertion on a suite with no judge model.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>One changed field of a draft.</summary>
public class AuthorChange
{
    /// <summary>The draft field name, as the client knows it (camelCase, e.g. "turns").</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Short human summary of the change, e.g. "3 turns -> 4 turns".</summary>
    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// The draft fields an authoring model is allowed to change.
///
/// A whitelist, and the merge copies everything else straight off the incoming draft, so a model
/// that omits a field from its answer cannot delete it -- the failure mode of returning a whole
/// document is silent data loss, and this is what removes it. The visible cost is that a model which
/// forgets to declare a field it meant to change makes no change at all, which the user sees and can
/// simply ask for again.
///
/// Four writable case fields are deliberately absent:
/// suiteId -- comes from the request, not from a model;
/// unmockedToolPolicy -- only Block is supported, and a model proposing Passthrough is exactly the
///   thing validation exists to stop;
/// sourceConversationId -- a provenance record, not authoring;
/// lastReviewedDate -- a human attestation that the case still reflects reality, which is worth
///   nothing if a model can stamp it.
/// </summary>
public static class AuthorFields
{
    public const string Name = "name";
    public const string Enabled = "enabled";
    public const string CaseType = "caseType";
    public const string EntryAgentId = "entryAgentId";
    public const string Turns = "turns";
    public const string Assertions = "assertions";
    public const string InitialStates = "initialStates";
    public const string History = "history";
    public const string Mocks = "mocks";
    public const string Priority = "priority";
    public const string Severity = "severity";
    public const string Batch = "batch";
    public const string CrossCutting = "crossCutting";
    public const string InvolvedAgents = "involvedAgents";
    public const string BusinessDomain = "businessDomain";
    public const string ExpectedOutcome = "expectedOutcome";

    public static readonly string[] All =
    [
        Name, Enabled, CaseType, EntryAgentId, Turns, Assertions, InitialStates, History, Mocks,
        Priority, Severity, Batch, CrossCutting, InvolvedAgents, BusinessDomain, ExpectedOutcome
    ];

    public static string? Normalize(string? value)
        => All.FirstOrDefault(f => string.Equals(f, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}
