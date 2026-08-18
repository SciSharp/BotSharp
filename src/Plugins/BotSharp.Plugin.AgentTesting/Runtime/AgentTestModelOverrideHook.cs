using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Settings;

namespace BotSharp.Plugin.AgentTesting.Runtime;

/// <summary>
/// Lets one test run force a specific model, which is what makes "sweep the same agent across
/// several models and compare them" possible.
///
/// Why an agent-load hook and not somewhere else: the main conversation path,
/// <c>RoutingService.InvokeAgent</c>, reads <c>agent.LlmConfig.Provider/Model</c> and passes them
/// EXPLICITLY to <c>CompletionProvider.GetChatCompletion</c>; and
/// <c>CompletionProvider.GetProviderAndModel</c> only consults the <c>provider</c>/<c>model</c>
/// conversation-state override when what it was passed is empty. In other words, writing those two
/// keys into conversation state has no effect on the main path -- the only moment left where an
/// override still takes is after LoadAgent has produced the agent and before InvokeAgent runs.
///
/// Structurally identical to TestMockExecutorProvider: look the conversation up in the registry, act
/// only on a hit, leave every other conversation untouched. No AsyncLocal, for the same reason as
/// MockFunctionExecutor (it is silently lost across a background queue or SideCar boundary).
///
/// SelfId is overridden to the empty string: AgentHookBase's default implementation throws, and this
/// hook has to apply to ANY agent under test -- including downstream agents a test conversation
/// reaches via route_to_agent. Those must run on the requested model too, otherwise a model
/// comparison only swaps the entry agent and the results mean nothing.
/// </summary>
public class AgentTestModelOverrideHook : AgentHookBase
{
    private readonly IAgentTestRunRegistry _registry;
    private readonly IConversationService _conversations;
    private readonly ILogger<AgentTestModelOverrideHook> _logger;

    public override string SelfId => string.Empty;

    public AgentTestModelOverrideHook(
        IServiceProvider services,
        AgentSettings settings,
        IAgentTestRunRegistry registry,
        IConversationService conversations,
        ILogger<AgentTestModelOverrideHook> logger)
        : base(services, settings)
    {
        _registry = registry;
        _conversations = conversations;
        _logger = logger;
    }

    public override Task OnAgentLoaded(Agent agent)
    {
        var active = _registry.TryGet(_conversations.ConversationId);
        var over = active?.ModelOverride;
        if (over == null || agent == null)
        {
            // Not a conversation under test, or this run named no model: touch nothing.
            return Task.CompletedTask;
        }

        // LlmConfig can be null (an agent.json with no llmConfig block) and the override still has
        // to apply there -- otherwise the agents that most need to be told which model to use are
        // exactly the ones it silently skips.
        agent.LlmConfig ??= new AgentLlmConfig();
        agent.LlmConfig.Provider = over.Provider;
        agent.LlmConfig.Model = over.Model;

        _logger.LogDebug(
            "Agent test run overrode agent {AgentId} to {Provider}/{Model} for conversation {ConversationId}.",
            agent.Id, over.Provider, over.Model, active!.ConversationId);

        return Task.CompletedTask;
    }
}
