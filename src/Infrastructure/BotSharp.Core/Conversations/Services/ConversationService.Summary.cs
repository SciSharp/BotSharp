using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<string> GetConversationSummary(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return string.Empty;

        var routing = _services.GetRequiredService<IRoutingService>();
        var agentService = _services.GetRequiredService<IAgentService>();

        var dialogs = _storage.GetDialogs(conversationId);
        if (dialogs.IsNullOrEmpty()) return string.Empty;

        var router = await agentService.LoadAgent(AIAssistant);
        var prompt = GetPrompt(router);
        var summary = await Summarize(router, prompt, dialogs);

        return summary;
    }

    private string GetPrompt(Agent agent)
    {
        var template = agent.Templates.First(x => x.Name == "conversation.summary").Content;
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object> { });
    }

    private async Task<string> Summarize(Agent agent, string prompt, List<RoleDialogModel> dialogs)
    {
        var provider = agent.LlmConfig.Provider;
        var model = agent.LlmConfig.Model;

        if (provider == null || model == null)
        {
            var agentSettings = _services.GetRequiredService<AgentSettings>();
            provider = agentSettings.LlmConfig.Provider;
            model = agentSettings.LlmConfig.Model;
        }

        var chatCompletion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model);
        var response = await chatCompletion.GetChatCompletions(new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = prompt
        }, dialogs);

        return response.Content;
    }
}
