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
    public List<TestTurn> Turns { get; set; } = [];
    public List<TestAssertion> Assertions { get; set; } = [];
    public List<TestState> InitialStates { get; set; } = [];
    public List<TestToolMock> Mocks { get; set; } = [];
    public string UnmockedToolPolicy { get; set; } = UnmockedToolPolicies.Block;
    public string? SourceConversationId { get; set; }
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
