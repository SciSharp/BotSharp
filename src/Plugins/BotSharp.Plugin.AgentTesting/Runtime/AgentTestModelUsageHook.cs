using BotSharp.Abstraction.Loggers;

namespace BotSharp.Plugin.AgentTesting.Runtime;

/// <summary>
/// Records which provider/model each completion in a test conversation actually ran on. Without a
/// model override the result's Provider/Model stay null ("agent default"), and the agent's LlmConfig
/// can change after the run -- so this is the only record of what that run really used.
///
/// Read after the call rather than off agent.LlmConfig at load time: LlmConfig may be empty, in
/// which case CompletionProvider falls back to a default the hook would otherwise never see.
/// </summary>
public class AgentTestModelUsageHook : IContentGeneratingHook
{
    private readonly IAgentTestRunRegistry _registry;
    private readonly IConversationService _conversations;

    public AgentTestModelUsageHook(IAgentTestRunRegistry registry, IConversationService conversations)
    {
        _registry = registry;
        _conversations = conversations;
    }

    public Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        // The llmJudge call shares the case's conversation id but runs under a Guid.Empty prompt
        // agent; it is the suite's judge model, not a model of the agent under test.
        if (message?.CurrentAgentId == Guid.Empty.ToString() || string.IsNullOrEmpty(tokenStats?.Model))
        {
            return Task.CompletedTask;
        }

        _registry.TryGet(_conversations.ConversationId)?.RecordModel($"{tokenStats.Provider}/{tokenStats.Model}");
        return Task.CompletedTask;
    }
}
