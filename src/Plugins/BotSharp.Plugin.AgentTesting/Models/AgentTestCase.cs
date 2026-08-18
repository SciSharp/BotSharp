using MongoDB.Bson.Serialization.Attributes;

namespace BotSharp.Plugin.AgentTesting.Models;

public class AgentTestCase : MongoBase
{
    public string SuiteId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Enabled { get; set; } = true;

    /// <summary>A length of 1 is a single-turn case.</summary>
    public List<TestTurn> Turns { get; set; } = [];

    /// <summary>Case-level assertions, evaluated once every turn has run.</summary>
    public List<TestAssertion> Assertions { get; set; } = [];

    /// <summary>Injected before the conversation starts; maps to BotSharp's MessageState.</summary>
    public List<TestState> InitialStates { get; set; } = [];

    public List<TestToolMock> Mocks { get; set; } = [];

    /// <summary>
    /// See <see cref="UnmockedToolPolicies"/>. Blocks by default: better a failing case than a
    /// real tool call.
    /// </summary>
    public string UnmockedToolPolicy { get; set; } = UnmockedToolPolicies.Block;

    /// <summary>The conversation this was recorded from, for traceability; null when hand-written.</summary>
    public string? SourceConversationId { get; set; }

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

public static class UnmockedToolPolicies
{
    public const string Block = "Block";
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
