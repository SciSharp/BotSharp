using MongoDB.Bson.Serialization.Attributes;

namespace BotSharp.Plugin.AgentTesting.Models;

public class AgentTestCase : MongoBase
{
    public string SuiteId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Enabled { get; set; } = true;

    /// <summary>长度 1 即单轮用例。</summary>
    public List<TestTurn> Turns { get; set; } = [];

    /// <summary>整案级断言：全部轮跑完后求值。</summary>
    public List<TestAssertion> Assertions { get; set; } = [];

    /// <summary>会话开始前注入，映射 BotSharp 的 MessageState。</summary>
    public List<TestState> InitialStates { get; set; } = [];

    public List<TestToolMock> Mocks { get; set; } = [];

    /// <summary>见 <see cref="UnmockedToolPolicies"/>。默认阻断，宁可用例失败也不真调工具。</summary>
    public string UnmockedToolPolicy { get; set; } = UnmockedToolPolicies.Block;

    /// <summary>录制来源会话，便于回溯；手写用例为 null。</summary>
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

    /// <summary>可选：入参子集匹配，用于同名工具多次调用给不同返回。</summary>
    public string? ArgsMatchJson { get; set; }

    /// <summary>可选：命中第 N 次调用（0 基）。</summary>
    public int? CallIndex { get; set; }

    /// <summary>假返回，写入 message.Content。</summary>
    public string ResultContent { get; set; } = string.Empty;

    /// <summary>模拟"中止本轮 LLM 续写"的真实行为。</summary>
    public bool StopCompletion { get; set; }

    /// <summary>
    /// mock 也要能写会话 state。大量 IFunctionCallback 不读 LLM 入参、完全靠
    /// IConversationStateService 跨轮传数据（见 docs/Architectures/IFunctionCallback-full-detail-report.md），
    /// 只 mock 返回值会让后续函数读不到 state 而全线崩。
    /// </summary>
    public List<TestState>? StateWrites { get; set; }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class TestAssertion
{
    /// <summary>outputContains|outputNotContains|outputRegex|toolCalled|toolNotCalled|stateEquals|routedToAgent|llmJudge</summary>
    public string Type { get; set; } = default!;

    /// <summary>函数名 / state key / agent 名。</summary>
    public string? Target { get; set; }

    /// <summary>期望值 / 正则 / 判官标准。</summary>
    public string? Expected { get; set; }

    /// <summary>toolCalled 的入参子集匹配。</summary>
    public string? ArgsMatchJson { get; set; }

    /// <summary>llmJudge 通过阈值。</summary>
    public double? MinScore { get; set; }

    /// <summary>失败则中止该用例后续轮。</summary>
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
