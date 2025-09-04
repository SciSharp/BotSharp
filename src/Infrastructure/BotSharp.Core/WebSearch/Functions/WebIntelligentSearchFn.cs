using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.WebSearch.Functions;

public class WebIntelligentSearchFn : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WebIntelligentSearchFn> _logger;

    public string Name => "util-web-intelligent_search";
    public string Indication => "Searching web";

    public WebIntelligentSearchFn(
        IServiceProvider services,
        ILogger<WebIntelligentSearchFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        var agentService = _services.GetRequiredService<IAgentService>();

        Agent? fromAgent = null;
        if (!string.IsNullOrEmpty(message.CurrentAgentId))
        {
            fromAgent = await agentService.GetAgent(message.CurrentAgentId);
        }

        var agent = new Agent
        {
            Id = fromAgent?.Id ?? BuiltInAgentId.UtilityAssistant,
            Name = fromAgent?.Name ?? "Utility Assistant",
            Instruction = "Please search the websites to handle user's request."
        };

        var dialogs = routingCtx.GetDialogs();
        if (dialogs.IsNullOrEmpty())
        {
            dialogs = conv.GetDialogHistory();
        }

        var response = await GetChatCompletion(agent, dialogs);
        message.Content = response;
        return true;
    }

    private async Task<string> GetChatCompletion(Agent agent, List<RoleDialogModel> dialogs)
    {
        try
        {
            var (provider, model) = GetLlmProviderModel();
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model);
            var response = await completion.GetChatCompletions(agent, dialogs);
            return response.Content;
        }
        catch (Exception ex)
        {
            var error = $"Error when searching web.";
            _logger.LogWarning(ex, $"{error}");
            return error;
        }
    }

    private (string, string) GetLlmProviderModel()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();

        var provider = state.GetState("web_search_llm_provider");
        var model = state.GetState("web_search_llm_model");

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-4o-mini-search-preview";

        var models = llmProviderService.GetProviderModels(provider);
        var foundModel = models.FirstOrDefault(x => x.WebSearch?.IsDefault == true)
                            ?? models.FirstOrDefault(x => x.WebSearch != null);

        model = foundModel?.Name ?? model;
        return (provider, model);
    }
}
