namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<bool> InstructStream(Agent agent, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var storage = _services.GetRequiredService<IConversationStorage>();
        storage.Append(conv.ConversationId, message);

        dialogs.Add(message);
        Context.SetDialogs(dialogs);

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.Push(agent.Id, "instruct directly");
        var agentId = routing.Context.GetCurrentAgentId();

        // Update next action agent's name
        var agentService = _services.GetRequiredService<IAgentService>();

        if (agent.Disabled)
        {
            var content = $"This agent ({agent.Name}) is disabled, please install the corresponding plugin ({agent.Plugin.Name}) to activate this agent.";

            message = RoleDialogModel.From(message, role: AgentRole.Assistant, content: content);
            dialogs.Add(message);
        }
        else
        {
            var provider = agent.LlmConfig.Provider;
            var model = agent.LlmConfig.Model;

            if (provider == null || model == null)
            {
                var agentSettings = _services.GetRequiredService<AgentSettings>();
                provider = agentSettings.LlmConfig.Provider;
                model = agentSettings.LlmConfig.Model;
            }

            var chatCompletion = CompletionProvider.GetChatCompletion(_services,
                provider: provider,
                model: model);

            await chatCompletion.GetChatCompletionsStreamingAsync(agent, dialogs, async data => { });
        }

        return true;
    }
}
