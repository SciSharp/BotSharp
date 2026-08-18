namespace BotSharp.Plugin.AgentTesting.Runtime;

public class TestMockExecutorProvider : IFunctionExecutorProvider
{
    private readonly IAgentTestRunRegistry _registry;
    private readonly IConversationService _conversations;
    private readonly IConversationStateService _state;
    private readonly ILogger<TestMockExecutorProvider> _logger;

    public TestMockExecutorProvider(
        IAgentTestRunRegistry registry,
        IConversationService conversations,
        IConversationStateService state,
        ILogger<TestMockExecutorProvider> logger)
    {
        _registry = registry;
        _conversations = conversations;
        _state = state;
        _logger = logger;
    }

    /// <summary>必须抢在内置解析链之前。</summary>
    public int Order => -1000;

    public IFunctionExecutor? TryResolve(string functionName, Agent agent)
    {
        var run = _registry.TryGet(_conversations.ConversationId);
        if (run == null)
        {
            return null;    // 非测试会话，完全放过
        }

        if (run.ForceBlockedFunctions.Contains(functionName))
        {
            return new MockFunctionExecutor(run, functionName, _state, _logger);
        }

        if (run.AllowedFunctions.Contains(functionName))
        {
            return null;    // 控制流：交给真实实现，否则 agent 走不动
        }

        // P1 only ever ships UnmockedToolPolicies.Block: every other function inside a test
        // conversation is taken over, mocked if a TestToolMock claims it, blocked otherwise (see
        // MockFunctionExecutor). A Passthrough policy existed here once -- it let an unmocked call
        // straight through to the real implementation on the theory that the runner would back-fill
        // an ObservedToolCall for it afterward from the conversation's dialogs. Nothing ever
        // implemented that back-fill, so toolNotCalled assertions vacuously passed against tools
        // that had genuinely executed with real side effects; the policy is now rejected at
        // case create/update (AgentTestController) rather than reachable here.
        return new MockFunctionExecutor(run, functionName, _state, _logger);
    }
}
