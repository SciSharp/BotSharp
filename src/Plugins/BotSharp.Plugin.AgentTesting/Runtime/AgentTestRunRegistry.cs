using System.Collections.Concurrent;

namespace BotSharp.Plugin.AgentTesting.Runtime;

/// <summary>
/// 一次正在执行的测试用例。按 conversationId 索引，因为测试上下文必须跨线程可靠——
/// AsyncLocal 在后台队列/SideCar 边界会静默丢失，而丢失意味着真实工具被执行。
/// </summary>
public class ActiveTestRun
{
    public string ConversationId { get; set; } = default!;
    public string CaseId { get; set; } = default!;

    /// <summary>
    /// The model this execution is forced onto; null = no override, use the agent's own LlmConfig.
    ///
    /// Applied by AgentTestModelOverrideHook, writing into agent.LlmConfig at the moment the agent
    /// finishes loading. That moment is the only one that works: the main conversation path,
    /// RoutingService.InvokeAgent, passes agent.LlmConfig.Provider/Model to CompletionProvider
    /// EXPLICITLY, and CompletionProvider.GetProviderAndModel only consults the conversation-state
    /// override when what it was passed is empty. So writing provider/model into conversation state
    /// has no effect on the main path, and overriding any later than agent load is already too late.
    /// </summary>
    public TestModel? ModelOverride { get; set; }

    public IReadOnlyList<TestToolMock> Mocks { get; set; } = [];
    public string UnmockedToolPolicy { get; set; } = UnmockedToolPolicies.Block;
    public ISet<string> AllowedFunctions { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ISet<string> ForceBlockedFunctions { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>当前在跑第几轮，用于把工具调用归到轮上。</summary>
    public int CurrentTurnIndex { get; set; }

    /// <summary>
    /// canary 是否被接管过。运行期的判据不是这个标志，而是 canary 调用返回的内容
    /// （见 AgentTestCaseRunner 里的说明）；这里留一个标志是为了能直接断言
    /// MockFunctionExecutor 认得 canary 函数名。
    /// </summary>
    public bool CanaryIntercepted { get; set; }

    private readonly List<ObservedToolCall> _observed = [];
    private readonly ConcurrentDictionary<string, int> _ordinals = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ObservedToolCall> ObservedCalls
    {
        get { lock (_observed) return _observed.ToList(); }
    }

    public void Record(ObservedToolCall call)
    {
        lock (_observed) _observed.Add(call);
    }

    /// <summary>同名函数第几次被调用（0 基），供 TestToolMock.CallIndex 匹配。</summary>
    public int NextCallOrdinal(string functionName)
        => _ordinals.AddOrUpdate(functionName, 0, (_, prev) => prev + 1);
}

public interface IAgentTestRunRegistry
{
    void Register(ActiveTestRun run);
    void Unregister(string conversationId);
    ActiveTestRun? TryGet(string? conversationId);
}

public class AgentTestRunRegistry : IAgentTestRunRegistry
{
    private readonly ConcurrentDictionary<string, ActiveTestRun> _runs = new();

    public void Register(ActiveTestRun run) => _runs[run.ConversationId] = run;

    public void Unregister(string conversationId) => _runs.TryRemove(conversationId, out _);

    public ActiveTestRun? TryGet(string? conversationId)
        => string.IsNullOrEmpty(conversationId) ? null
           : _runs.TryGetValue(conversationId, out var run) ? run : null;
}

/// <summary>
/// 默认放行的控制流函数。**不要改成按 `util-` 前缀匹配**：`util-email-handle_email_sender`、
/// `util-twilio-outbound_phone_call`、`util-twilio-text_message`、`util-http-handle_http_request`、
/// `util-db-sql_select` 都是 `util-` 开头且有真副作用，按前缀放行等于测试跑一遍真发邮件真打电话。
/// </summary>
public static class ControlFlowFunctions
{
    public static readonly IReadOnlySet<string> Default = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "route_to_agent",
        "response_to_user",
        "human_intervention_needed",
        "util-routing-fallback_to_router",
        "util-instruct-execute_template"
    };
}

public static class AgentTestCanary
{
    /// <summary>
    /// 运行器开跑前会调一次这个函数名验证接缝真的生效。若 BotSharp.Core 走的是没有
    /// IFunctionExecutorProvider 支持的旧包，mock 会静默失效——canary 把它变成显式失败。
    /// </summary>
    public const string FunctionName = "__agent_test_canary__";

    /// <summary>
    /// 接管方（MockFunctionExecutor）写入、判定方（BotSharpAgentConversationDriver）比对的
    /// 同一枚哨兵值——两处各存一份裸字面量 "canary" 是这条安全关键判定曾经存在的一处隐患
    /// （即便跑偏也只会让每个用例都报 Error，不会静默放过，但仍然只该有一份定义）。
    /// </summary>
    public const string ExpectedContent = "canary";
}

/// <summary>
/// Marks every conversation this harness creates as synthetic, so an operator who stumbles on a
/// strange conversation in the admin UI (or an analytics job aggregating over the collection) can
/// tell it apart from a genuine customer conversation. AgentTestCaseResult.ConversationId already
/// gives forward traceability (result -> conversation); this is the reverse direction.
///
/// Deliberately just ONE marker (the tag), not a Channel/state-key marker too -- a prior version
/// of this class also declared a Channel constant seeded as the "channel" conversation state, but
/// that key (CustomStateKeys.Channel) is load-bearing production state with ~30 readers across
/// this codebase (routing hooks, visibility gates, etc.), none of which recognize a synthetic
/// value, and AgentTestRecorder.BuildDraft copies every seeded state into a recorded case's own
/// InitialStates -- so it also silently overwrote a recorded case's REAL channel on every
/// recording. The tag alone already satisfies the "make it identifiable after the fact" goal
/// without touching anything BotSharp/onebrain branches on. Do not add another state-key marker
/// without grepping every reader of that key first.
/// </summary>
public static class AgentTestConversationMarker
{
    /// <summary>
    /// The SAME tag value BotSharp-UI's chat window already writes when a human manually tags a
    /// real conversation as a test conversation (ConversationTag.Test in the UI's
    /// src/lib/helpers/enums.js) -- reusing it means any current or future code that filters this
    /// tag out of normal conversation views/analytics already covers harness-created conversations
    /// too, with nothing new to teach it.
    /// </summary>
    public const string Tag = "test-set";
}
