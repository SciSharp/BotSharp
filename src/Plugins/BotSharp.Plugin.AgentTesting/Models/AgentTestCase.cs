using MongoDB.Bson.Serialization.Attributes;

namespace BotSharp.Plugin.AgentTesting.Models;

public class AgentTestCase : MongoBase
{
    public string SuiteId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// See <see cref="CaseTypes"/>. Defaults to Agent, which is also what every case stored before
    /// this field existed deserialises to -- a missing BSON element leaves the property at this
    /// initialiser, so old documents read back as Agent cases rather than as an invalid blank.
    ///
    /// Not cosmetic: a Routing case is validated differently (single turn, must actually assert a
    /// routing outcome, no llmJudge -- see CaseValidation.Validate) and is the only
    /// type counted towards a run's routing accuracy.
    /// </summary>
    public string CaseType { get; set; } = CaseTypes.Agent;

    /// <summary>
    /// The agent the conversation is opened on, overriding the suite's own AgentId. Null uses the
    /// suite's.
    ///
    /// This is what makes Routing and Agent cases separable without one suite per entry point.
    /// BotSharp dispatches on the agent's own type: ConversationService.SendMessage sends a Routing
    /// agent through RoutingService.InstructLoop (the router runs, and can hand off) and everything
    /// else through InstructDirect (straight into that agent, router never consulted). So pointing a
    /// case at the Copilot entry agent tests routing, and pointing it at a leaf agent tests that
    /// agent in isolation -- the two modes the evaluation framework calls Routing and Agent cases.
    /// </summary>
    public string? EntryAgentId { get; set; }

    /// <summary>A length of 1 is a single-turn case.</summary>
    public List<TestTurn> Turns { get; set; } = [];

    /// <summary>Case-level assertions, evaluated once every turn has run.</summary>
    public List<TestAssertion> Assertions { get; set; } = [];

    /// <summary>Injected before the conversation starts; maps to BotSharp's MessageState.</summary>
    public List<TestState> InitialStates { get; set; } = [];

    /// <summary>
    /// Prior conversation turns written into the conversation before the case's own turns run, so a
    /// real question-and-answer exchange can be replayed as the fixed starting context for a test.
    ///
    /// Different from <see cref="InitialStates"/>: states are the machine-readable context an agent
    /// reads, this is the dialogue the model sees. It is what makes "given the resident already told
    /// us the fridge is leaking, does asking for an ETA still route correctly" expressible without
    /// re-driving those turns through the model on every run -- which would be slow, would cost
    /// tokens, and would make the preamble itself a source of flakiness.
    ///
    /// Never counted in a result's AgentChain: these messages are authored, not something the agent
    /// under test did, and letting them in would fail an exact chain assertion for a reason the
    /// author did not cause.
    /// </summary>
    public List<TestHistoryMessage> History { get; set; } = [];

    public List<TestToolMock> Mocks { get; set; } = [];

    /// <summary>
    /// See <see cref="UnmockedToolPolicies"/>. Blocks by default: better a failing case than a
    /// real tool call.
    /// </summary>
    public string UnmockedToolPolicy { get; set; } = UnmockedToolPolicies.Block;

    /// <summary>The conversation this was recorded from, for traceability; null when hand-written.</summary>
    public string? SourceConversationId { get; set; }

    /// <summary>
    /// See <see cref="CasePriorities"/>. Decides which batch the case runs in, and therefore whether
    /// a failure stops the evaluation (batch 1) or is merely reported (batch 3).
    ///
    /// Defaults to P1, which is what every case stored before this field existed reads back as. P0
    /// would put all of them in the stop-loss batch, where one failure halts everything; P2 would
    /// drop them out of the mandatory batches altogether. P1 is mandatory but not stop-loss, which is
    /// the honest position for a case nobody has triaged yet.
    /// </summary>
    public string Priority { get; set; } = CasePriorities.P1;

    /// <summary>
    /// See <see cref="CaseSeverities"/>. What a failure of this case means, as opposed to how urgent
    /// it is to run.
    ///
    /// Defaults to S1 for the same reason as Priority: S0 would make every untriaged legacy failure
    /// an immediate no-go, and S2 would let a genuine safety failure hide as an experience nit.
    /// </summary>
    public string Severity { get; set; } = CaseSeverities.S1;

    /// <summary>
    /// Overrides the batch derived from <see cref="Priority"/> and <see cref="CrossCutting"/>; null
    /// uses the derivation. See <see cref="CaseBatches.Effective"/>.
    /// </summary>
    public int? Batch { get; set; }

    /// <summary>
    /// A cross-cutting case runs in EVERY evaluation scope, whatever the change was. That is what
    /// safety cases are: the scope-narrowing rules exist to save time on cases a change cannot
    /// affect, and a claim that a change cannot affect safety is exactly the claim not to take on
    /// trust.
    /// </summary>
    public bool CrossCutting { get; set; }

    /// <summary>
    /// Agents this case actually exercises, as ids. Empty is the normal state and does NOT mean "no
    /// agents": the effective set then falls back to the case's entry agent, which is already known.
    /// See <see cref="CaseScope.InvolvedAgentIds"/>.
    ///
    /// Worth filling in for a routing case, where the entry agent is the router and the agents that
    /// matter are the ones downstream of it -- those cannot be inferred from the case definition,
    /// only observed by running it.
    /// </summary>
    public List<string> InvolvedAgents { get; set; } = [];

    /// <summary>Business domain, for pulling a subset by business line rather than by agent.</summary>
    public string? BusinessDomain { get; set; }

    /// <summary>
    /// What this case is supposed to achieve, in business terms, for whoever reviews the result.
    /// Deliberately free text and never evaluated: an expected outcome a machine could check is an
    /// assertion, and belongs in <see cref="Assertions"/> where it will actually be enforced.
    /// </summary>
    public string? ExpectedOutcome { get; set; }

    /// <summary>
    /// When a human last confirmed this case still reflects reality. Null means never. Not touched by
    /// editing or running the case -- a case can be edited many times and still be built on an
    /// assumption nobody has questioned in a year, and conflating the two would hide exactly that.
    /// </summary>
    public DateTime? LastReviewedDate { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements(Inherited = true)]
public class TestTurn
{
    public int Index { get; set; }
    public string UserMessage { get; set; } = default!;
    public List<TestAssertion> Assertions { get; set; } = [];
}

[BsonIgnoreExtraElements(Inherited = true)]
public class TestState
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public int ActiveRounds { get; set; } = -1;
    public bool Global { get; set; }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class TestToolMock
{
    public string FunctionName { get; set; } = default!;

    /// <summary>
    /// Optional argument-subset match, for giving different returns to repeated calls of the same
    /// tool.
    /// </summary>
    public string? ArgsMatchJson { get; set; }

    /// <summary>Optional: match only the Nth call (0-based).</summary>
    public int? CallIndex { get; set; }

    /// <summary>The faked return, written to message.Content.</summary>
    public string ResultContent { get; set; } = string.Empty;

    /// <summary>Reproduces a real tool's "stop this turn's LLM completion" behaviour.</summary>
    public bool StopCompletion { get; set; }

    /// <summary>
    /// A mock has to be able to write conversation state too. Plenty of IFunctionCallback
    /// implementations ignore the LLM's arguments entirely and pass data across turns purely
    /// through IConversationStateService, so mocking only the return value leaves every later
    /// function unable to read what it expects and the whole case collapses.
    /// </summary>
    public List<TestState>? StateWrites { get; set; }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class TestAssertion
{
    /// <summary>outputContains|outputNotContains|outputRegex|toolCalled|toolNotCalled|stateEquals|routedToAgent|llmJudge</summary>
    public string Type { get; set; } = default!;

    /// <summary>Function name / state key / agent name.</summary>
    public string? Target { get; set; }

    /// <summary>Expected value / regex / judging criteria.</summary>
    public string? Expected { get; set; }

    /// <summary>Argument-subset match for toolCalled.</summary>
    public string? ArgsMatchJson { get; set; }

    /// <summary>Pass threshold for llmJudge.</summary>
    public double? MinScore { get; set; }

    /// <summary>On failure, abort the remaining turns of this case.</summary>
    public bool Fatal { get; set; }
}

/// <summary>
/// One authored message in a case's <see cref="AgentTestCase.History"/>. Deliberately just a role
/// and text: a mocked tool call belongs in <see cref="AgentTestCase.Mocks"/>, and a fabricated
/// function-call dialog would let a case claim a tool ran when nothing did.
/// </summary>
[BsonIgnoreExtraElements(Inherited = true)]
public class TestHistoryMessage
{
    /// <summary>See <see cref="HistoryRoles"/>.</summary>
    public string Role { get; set; } = HistoryRoles.User;

    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Roles an authored history message may take. Only these two: a system message would compete with
/// the agent's own instruction, and a function message would fake a tool call.
/// </summary>
public static class HistoryRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";

    public static readonly string[] All = [User, Assistant];

    /// <summary>Canonical role for any casing; null for anything unsupported, so it is rejected.</summary>
    public static string? Normalize(string? value)
        => All.FirstOrDefault(r => string.Equals(r, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class UnmockedToolPolicies
{
    public const string Block = "Block";
}

/// <summary>
/// How urgent it is to run a case, which is what decides its batch. Distinct from
/// <see cref="CaseSeverities"/>: priority is about scheduling, severity is about consequence.
/// </summary>
public static class CasePriorities
{
    public const string P0 = "P0";
    public const string P1 = "P1";
    public const string P2 = "P2";

    public static readonly string[] All = [P0, P1, P2];

    public static string? Normalize(string? value)
        => All.FirstOrDefault(p => string.Equals(p, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// What a failure of this case means.
///
/// S0 -- zero tolerance. Data leakage, an unauthorised action taken without confirmation, a missed
///       critical escalation. One of these is a stop, not a statistic.
/// S1 -- non-inferiority. Wrong routing, wrong tool, a fabricated fact, a wrong business state:
///       allowed to move within a threshold, not allowed to get worse than it.
/// S2 -- experience quality. Phrasing, repetition, awkward hand-offs. Must never be able to mask an
///       S0 or S1 result.
/// </summary>
public static class CaseSeverities
{
    public const string S0 = "S0";
    public const string S1 = "S1";
    public const string S2 = "S2";

    public static readonly string[] All = [S0, S1, S2];

    public static string? Normalize(string? value)
        => All.FirstOrDefault(s => string.Equals(s, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Which batch a case belongs to. Batches run in order and exist to stop early: batch 1 is the
/// stop-loss batch, batch 3 does not block a release decision.
/// </summary>
public static class CaseBatches
{
    public const int StopLoss = 1;
    public const int Mandatory = 2;
    public const int Optional = 3;

    public static readonly int[] All = [StopLoss, Mandatory, Optional];

    /// <summary>
    /// An explicit <see cref="AgentTestCase.Batch"/> wins. Otherwise a cross-cutting case is batch 1
    /// whatever its priority -- a safety case that only runs after everything else has passed cannot
    /// stop anything -- and priority maps P0/P1/P2 onto 1/2/3.
    /// </summary>
    public static int Effective(AgentTestCase testCase)
    {
        if (testCase.Batch is { } explicitBatch && All.Contains(explicitBatch))
        {
            return explicitBatch;
        }

        if (testCase.CrossCutting)
        {
            return StopLoss;
        }

        return testCase.Priority switch
        {
            CasePriorities.P0 => StopLoss,
            CasePriorities.P2 => Optional,
            _ => Mandatory
        };
    }
}

/// <summary>
/// What a case is verifying, which decides how it is validated and how it is aggregated.
///
/// Routing -- single turn from the entry agent, asserting only which agent took the conversation.
///            Carries no quality judgement: the whole verdict is "expected agent == actual agent".
/// Agent   -- one agent's own behaviour, normally entered directly so the router is not part of what
///            is being measured. Multi-agent journeys are Agent cases too; the agentChain assertion
///            is what describes their hand-offs.
/// </summary>
public static class CaseTypes
{
    public const string Routing = "Routing";
    public const string Agent = "Agent";

    public static readonly string[] All = [Routing, Agent];

    /// <summary>
    /// Maps any casing of a known type onto its canonical constant; null for anything unknown, so
    /// the caller can reject it rather than storing a value nothing else will ever match.
    /// </summary>
    public static string? Normalize(string? value)
        => All.FirstOrDefault(t => string.Equals(t, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class AgentTestStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Passed = "Passed";
    public const string Failed = "Failed";
    public const string Error = "Error";
    public const string Cancelled = "Cancelled";
}
