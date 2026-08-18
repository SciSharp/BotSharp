using MongoDB.Bson.Serialization.Attributes;

namespace BotSharp.Plugin.AgentTesting.Models;

public class AgentTestCaseResult : MongoBase
{
    public string RunId { get; set; } = default!;
    public string CaseId { get; set; } = default!;
    public string CaseName { get; set; } = default!;

    /// <summary>Passed | Failed | Error | Cancelled -- see <see cref="AgentTestStatus"/>.</summary>
    public string Status { get; set; } = AgentTestStatus.Pending;

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

    public long DurationMs { get; set; }

    /// <summary>
    /// Infrastructure-level reason for failure (a timeout, a dead canary), kept distinct from an
    /// assertion failure.
    /// </summary>
    public string? Error { get; set; }

    public List<TurnResult> Turns { get; set; } = [];

    /// <summary>Case-level assertion results.</summary>
    public List<AssertionResult> Assertions { get; set; } = [];

    public List<ObservedToolCall> ObservedToolCalls { get; set; } = [];

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements(Inherited = true)]
public class TurnResult
{
    public int Index { get; set; }
    public string UserMessage { get; set; } = default!;
    public string? Output { get; set; }
    public List<AssertionResult> Assertions { get; set; } = [];
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
