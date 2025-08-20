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
            Name = fromAgent?.Name ?? "AI Agent",
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
            var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
            var completion = CompletionProvider.GetChatCompletion(_services, provider: "openai", model: "gpt-4o-search-preview");
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
}
