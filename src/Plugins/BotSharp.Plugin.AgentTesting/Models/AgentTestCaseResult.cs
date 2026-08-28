using MongoDB.Bson.Serialization.Attributes;

namespace BotSharp.Plugin.AgentTesting.Models;

public class AgentTestCaseResult : MongoBase
{
    public string RunId { get; set; } = default!;
    public string CaseId { get; set; } = default!;
    public string CaseName { get; set; } = default!;

    /// <summary>Passed | Failed | Error | Cancelled -- see <see cref="AgentTestStatus"/>.</summary>
    public string Status { get; set; } = AgentTestStatus.Pending;

    /// <summary>
    /// Copied off the case so a result is self-describing -- see <see cref="CaseTypes"/>. Aggregating
    /// by type (routing accuracy separately from agent pass rate, as the evaluation framework gates
    /// them separately) has to work from the result rows alone, without re-reading cases that may
    /// have been edited or deleted since the run.
    /// </summary>
    public string CaseType { get; set; } = CaseTypes.Agent;

    /// <summary>The conversation this execution created; live conversations are never reused.</summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Which model produced this result. Null = the agent's own LlmConfig was used (the run named
    /// no models). Unlike the deleted AgentTestRunTriggerRequest.Provider/Model, these values were
    /// genuinely applied to this execution by AgentTestModelOverrideHook -- they are not a
    /// decorative record of something that never took effect.
    /// </summary>
    public string? Provider { get; set; }
    public string? Model { get; set; }

    /// <summary>
    /// Wall clock for the whole case, including the canary, the mock lookups and the conversation
    /// reads. Comparable between models because every model pays the same overhead, but it is not a
    /// model-latency measurement -- see <see cref="ModelDurationMs"/> for that.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Time spent inside the agent calls alone, summed over the turns. This is what a latency gate
    /// should be read against: <see cref="DurationMs"/> also contains harness work, and on a fast
    /// case that overhead is a large enough share to move a percentile.
    /// </summary>
    public long ModelDurationMs { get; set; }

    /// <summary>
    /// Tokens this case consumed, measured as the delta across its own execution rather than an
    /// absolute reading, so it stays correct even when the statistics service outlives one case.
    ///
    /// Total only: the input/output split lives in TokenStatistics' private fields and is not
    /// reachable through ITokenStatistics, which exposes Total, Cost and AccumulatedCost. Recording a
    /// guessed split would be worse than recording none.
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// What those tokens cost, priced by the model's own configured unit costs. Comparable only
    /// within a run, or across runs whose pricing snapshot matches -- see
    /// <see cref="AgentTestRun.ModelPricing"/>.
    /// </summary>
    public double Cost { get; set; }

    /// <summary>
    /// Infrastructure-level reason for failure (a timeout, a dead canary), kept distinct from an
    /// assertion failure.
    /// </summary>
    public string? Error { get; set; }

    public List<TurnResult> Turns { get; set; } = [];

    /// <summary>Case-level assertion results.</summary>
    public List<AssertionResult> Assertions { get; set; } = [];

    public List<ObservedToolCall> ObservedToolCalls { get; set; } = [];

    /// <summary>
    /// Every agent that produced an assistant message over the whole case, in order, with
    /// consecutive repeats collapsed -- so ["Copilot", "WorkOrder"] means the entry agent answered
    /// and then handed off once, however many messages each of them emitted.
    ///
    /// This is the only record of the hand-offs. route_to_agent is on the allow list
    /// (AgentTestRunRegistry) and therefore never reaches MockFunctionExecutor, which is the only
    /// caller of ActiveTestRun.Record -- so routing decisions produce no ObservedToolCall and would
    /// otherwise be invisible. Reconstructed instead from the conversation's own assistant dialogs,
    /// each of which carries the agent that wrote it.
    /// </summary>
    public List<string> AgentChain { get; set; } = [];

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements(Inherited = true)]
public class TurnResult
{
    public int Index { get; set; }
    public string UserMessage { get; set; } = default!;
    public string? Output { get; set; }
    public List<AssertionResult> Assertions { get; set; } = [];

    /// <summary>
    /// Time the agent call for this turn took. Excludes the assertion evaluation and the conversation
    /// reads that follow it, so summing this over the turns gives a latency figure that does not
    /// drift as the harness itself gains work.
    /// </summary>
    public long ModelDurationMs { get; set; }

    /// <summary>
    /// The agents that answered during THIS turn only, in order, consecutive repeats collapsed. The
    /// case-level <see cref="AgentTestCaseResult.AgentChain"/> is the whole conversation; this is
    /// the slice added by this turn, which is what makes "the second turn should have stayed with
    /// the same agent" expressible.
    /// </summary>
    public List<string> AgentChain { get; set; } = [];
}

[BsonIgnoreExtraElements(Inherited = true)]
public class AssertionResult
{
    public string Type { get; set; } = default!;
    public string? Target { get; set; }
    public string? Expected { get; set; }
    public string? Actual { get; set; }
    public bool Passed { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// The numeric score, on llmJudge results only; null everywhere else. Actual already carries it
    /// as text for display, but a quality gate has to average scores across a run, and re-parsing a
    /// display string to do arithmetic is how a formatting change quietly breaks a gate.
    /// </summary>
    public double? Score { get; set; }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class ObservedToolCall
{
    public int TurnIndex { get; set; }
    public string FunctionName { get; set; } = default!;
    public string? ArgsJson { get; set; }

    /// <summary>Mocked | Blocked</summary>
    public string Outcome { get; set; } = default!;

    public string? ResultContent { get; set; }
}
