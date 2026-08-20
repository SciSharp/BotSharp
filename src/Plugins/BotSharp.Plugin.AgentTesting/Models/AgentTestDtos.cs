namespace BotSharp.Plugin.AgentTesting.Models;

/// <summary>
/// Body for creating/updating a suite via POST/PUT. Id/CreateDate/UpdateDate are server-owned and
/// deliberately absent: the repository generates them on create, and on update the controller
/// carries them over from the stored entity.
/// </summary>
public class AgentTestSuiteUpsertRequest
{
    public string AgentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Nullable so a request that omits "enabled" (a partial PUT, e.g. one that only means to
    /// change caseTimeoutSeconds) can be told apart from one that explicitly sends
    /// <c>"enabled": true</c> -- a non-nullable bool defaulting to true made both cases look
    /// identical, so a partial PUT against a suite someone had deliberately disabled silently
    /// re-enabled it (see AgentTestController.ApplySuite, which now does
    /// <c>request.Enabled ?? suite.Enabled</c>). Null on create still means "enabled" -- a brand
    /// new AgentTestSuite's own Enabled default (true) is what the null falls back to there.
    /// </summary>
    public bool? Enabled { get; set; }
    public string? JudgeProvider { get; set; }
    public string? JudgeModel { get; set; }
    public List<string> ExtraAllowedFunctions { get; set; } = [];
    public List<string> ForceBlockedFunctions { get; set; } = [];
    public int CaseTimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// Body for creating/updating a case via POST/PUT; the fields map straight onto the writable part
/// of AgentTestCase.
/// </summary>
public class AgentTestCaseUpsertRequest
{
    public string SuiteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// See <see cref="CaseTypes"/>. Blank or omitted means Agent, so an existing client that never
    /// sends this field keeps creating exactly the cases it created before. Any other unrecognised
    /// value is a 400 rather than a silent fallback -- storing "Rounting" as Agent would leave the
    /// author with a case that looks routing-shaped and is never counted as one.
    /// </summary>
    public string? CaseType { get; set; }

    /// <summary>
    /// Authored prior turns, replayed as the case's opening context. See
    /// <see cref="AgentTestCase.History"/>.
    /// </summary>
    public List<TestHistoryMessage> History { get; set; } = [];

    /// <summary>
    /// Optional entry agent, overriding the suite's. See
    /// <see cref="AgentTestCase.EntryAgentId"/> for why this is the switch between testing routing
    /// and testing one agent in isolation. Validated to exist at save time: a typo here would
    /// otherwise turn every run of the case into an opaque infrastructure Error.
    /// </summary>
    public string? EntryAgentId { get; set; }

    public List<TestTurn> Turns { get; set; } = [];
    public List<TestAssertion> Assertions { get; set; } = [];
    public List<TestState> InitialStates { get; set; } = [];
    public List<TestToolMock> Mocks { get; set; } = [];
    public string UnmockedToolPolicy { get; set; } = UnmockedToolPolicies.Block;
    public string? SourceConversationId { get; set; }

    /// <summary>See <see cref="CasePriorities"/>. Blank or omitted keeps P1.</summary>
    public string? Priority { get; set; }

    /// <summary>See <see cref="CaseSeverities"/>. Blank or omitted keeps S1.</summary>
    public string? Severity { get; set; }

    /// <summary>Explicit batch override; null derives it from priority and the cross-cutting flag.</summary>
    public int? Batch { get; set; }

    public bool CrossCutting { get; set; }

    /// <summary>Agent ids; empty falls back to the case's entry agent. See <see cref="CaseScope"/>.</summary>
    public List<string> InvolvedAgents { get; set; } = [];

    public string? BusinessDomain { get; set; }
    public string? ExpectedOutcome { get; set; }

    /// <summary>
    /// Sent explicitly by whoever reviewed the case. Never set by the server on save: a case can be
    /// edited many times and still rest on an assumption nobody has questioned, and stamping this on
    /// every write would hide precisely that.
    /// </summary>
    public DateTime? LastReviewedDate { get; set; }
}

/// <summary>
/// Body of POST /agent-test/scope -- work out which cases a change needs to run, before running
/// anything. See <see cref="BotSharp.Plugin.AgentTesting.Services.CaseScope"/>.
/// </summary>
public class ScopeSelectionRequest
{
    /// <summary>Agent ids the change touches. Ignored when <see cref="FullPlatform"/> is set.</summary>
    public List<string> TargetAgentIds { get; set; } = [];

    /// <summary>A platform-wide change: every enabled case is in scope.</summary>
    public bool FullPlatform { get; set; }

    /// <summary>Narrow to one batch (1, 2 or 3); null covers all of them.</summary>
    public int? Batch { get; set; }
}

/// <summary>
/// One case's place in a scope. Carries the metadata the decision was made from, not just the verdict,
/// so the scope can be reviewed rather than taken on trust.
/// </summary>
public class ScopedCaseDto
{
    public string CaseId { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public string SuiteId { get; set; } = string.Empty;
    public string SuiteName { get; set; } = string.Empty;
    public string CaseType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool CrossCutting { get; set; }
    public bool Enabled { get; set; }

    /// <summary>The effective batch, after the priority and cross-cutting derivation.</summary>
    public int Batch { get; set; }

    /// <summary>The set the decision was made against, authored or derived from the entry agent.</summary>
    public List<string> InvolvedAgentIds { get; set; } = [];

    /// <summary>See <see cref="BotSharp.Plugin.AgentTesting.Services.ScopeReasons"/>.</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// The answer to "what will this change actually test".
///
/// Both lists are returned on purpose. A scope report that only showed what it included would let a
/// change be signed off against a set of cases that quietly left out the interesting one, and an
/// excluded case produces no result to notice -- which is why the exclusions, with their reasons, are
/// the half worth reading.
/// </summary>
public class ScopeSelectionResponse
{
    public List<string> TargetAgentIds { get; set; } = [];
    public bool FullPlatform { get; set; }
    public int? Batch { get; set; }

    /// <summary>Every registered case, whatever its state -- the denominator for coverage.</summary>
    public int TotalCases { get; set; }

    public List<ScopedCaseDto> Included { get; set; } = [];
    public List<ScopedCaseDto> Excluded { get; set; } = [];
}

/// <summary>
/// Body of POST /agent-test/record -- record a draft case from a real conversation, see
/// <see cref="BotSharp.Plugin.AgentTesting.Services.AgentTestRecorder.LoadAndBuildAsync"/>.
/// </summary>
public class AgentTestRecordRequest
{
    public string SuiteId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Optional: use this model to split the conversation into one or more scenarios, each becoming
    /// its own draft case. Null calls no model at all and falls back to the deterministic recorder
    /// (whole conversation = one case).
    ///
    /// The model ONLY decides where to split and what to name each case. Mock return values,
    /// toolCalled/stateEquals assertions and state writes still come verbatim from the real
    /// conversation -- see ICaseSegmenter for why that boundary is drawn there.
    ///
    /// Note that setting this sends the conversation's user messages and tool names to that model
    /// vendor (tool arguments and results are withheld). The deterministic recorder never leaves
    /// this system, so using this field means accepting one data egress.
    /// </summary>
    public TestModel? Model { get; set; }
}

/// <summary>
/// Body of POST /agent-test/suites/{id}/run.
///
/// CaseIds lands on AgentTestRun.CaseIds, which AgentTestRunExecutor uses to narrow the suite's
/// enabled cases further (null/empty = no filter, run every enabled case, identical to the field not
/// existing). "Re-run just the ones that failed" is a core regression-harness scenario, not a
/// nice-to-have.
///
/// Fix wave (project owner decision): this used to also accept Provider/Model, stored verbatim
/// on AgentTestRun.Provider/Model for "which model did this run use" auditing -- but nothing ever
/// read them back to actually apply a model override to execution (the run always executed with
/// the agent's own LlmConfig regardless of what was passed here). A permanent record that
/// CLAIMS a model was used, when it wasn't, is worse than no field at all, so both fields were
/// deleted rather than left to silently lie. Implementing a real override would need a channel
/// through IAgentConversationDriver, which is out of scope here.
/// </summary>
public class AgentTestRunTriggerRequest
{
    public List<string>? CaseIds { get; set; }

    /// <summary>
    /// Models to sweep for this run; null/empty = one pass on the agent's own LlmConfig (the
    /// existing behaviour).
    ///
    /// This is not a revival of the two deleted Provider/Model fields above: those were stored and
    /// never read back, so they lied. This one genuinely takes effect --
    /// AgentTestModelOverrideHook rewrites agent.LlmConfig to the named provider/model as the agent
    /// loads, so the main conversation path (RoutingService.InvokeAgent) reads the rewritten value,
    /// and every AgentTestCaseResult records which model produced it.
    /// </summary>
    public List<TestModel>? Models { get; set; }
}

/// <summary>
/// Body of GET /agent-test/runs/{id}: one run plus every AgentTestCaseResult belonging to it.
/// </summary>
public class AgentTestRunDetailDto
{
    public AgentTestRun Run { get; set; } = default!;
    public List<AgentTestCaseResult> Results { get; set; } = [];
}
