using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<string> GetConversationSummary(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty()) return string.Empty;

        var routing = _services.GetRequiredService<IRoutingService>();
        var agentService = _services.GetRequiredService<IAgentService>();

        var contents = new List<string>();
        foreach ( var conversationId in conversationIds)
        {
            if (string.IsNullOrEmpty(conversationId)) continue;

            var dialogs = _storage.GetDialogs(conversationId);

            if (dialogs.IsNullOrEmpty()) continue;

            var content = GetConversationContent(dialogs);
            if (string.IsNullOrWhiteSpace(content)) continue;

            contents.Add(content);
        }

        if (contents.IsNullOrEmpty()) return string.Empty;

        var router = await agentService.LoadAgent(AIAssistant);
        var prompt = GetPrompt(router, contents);
        var summary = await Summarize(router, prompt);

        return summary;
    }

    private string GetPrompt(Agent agent, List<string> contents)
    {
        var template = agent.Templates.First(x => x.Name == "conversation.summary").Content;
        var render = _services.GetRequiredService<ITemplateRender>();

        var texts = new List<string>();
        for (int i = 0; i < contents.Count; i++)
        {
            texts.Add($"{contents[i]}");
        }

        return render.Render(template, new Dictionary<string, object>
        {
            { "texts", texts }
        });
    }

    private async Task<string> Summarize(Agent agent, string prompt)
    {
        var provider = "openai";
        string? model;

        var providerService = _services.GetRequiredService<ILlmProviderService>();
        var modelSettings = providerService.GetProviderModels(provider);
        var modelSetting = modelSettings.FirstOrDefault(x => x.Name.IsEqualTo("gpt4-turbo") || x.Name.IsEqualTo("gpt-4o"));

        if (modelSetting != null)
        {
            model = modelSetting.Name;
        }
        else
        {
            provider = agent?.LlmConfig?.Provider;
            model = agent?.LlmConfig?.Model;
            if (provider == null || model == null)
            {
                var agentSettings = _services.GetRequiredService<AgentSettings>();
                provider = agentSettings.LlmConfig.Provider;
                model = agentSettings.LlmConfig.Model;
            }
        }

        var chatCompletion = CompletionProvider.GetChatCompletion(_services, provider, model);
        var response = await chatCompletion.GetChatCompletions(new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = prompt
        }, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, "Please summarize the conversations.")
        });

        return response.Content;
    }

    private string GetConversationContent(List<RoleDialogModel> dialogs, int maxDialogCount = 100)
    {
        var conversation = "";

        foreach (var dialog in dialogs.TakeLast(maxDialogCount))
        {
            var role = dialog.Role;
            if (role == AgentRole.Function) continue;

            if (role != AgentRole.User)
            {
                role = AgentRole.Assistant;
            }

            conversation += $"{role}: {dialog.Payload ?? dialog.Content}\r\n";
        }

        if (string.IsNullOrEmpty(conversation))
        {
            return string.Empty;
        }

        return conversation + "\r\n";
    }
}
