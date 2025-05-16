using BotSharp.Abstraction.Hooks;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace BotSharp.Plugin.DeepSeek.Providers.Text;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TextCompletionProvider> _logger;
    protected string _model;

    public string Provider => "deepseek-ai";
    public string Model => _model;

    public TextCompletionProvider(
        IServiceProvider services,
        ILogger<TextCompletionProvider> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<string> GetCompletion(string text, string agentId, string messageId)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agentId);
        var state = _services.GetRequiredService<IConversationStateService>();

        // Before chat completion hook
        var agent = new Agent()
        {
            Id = agentId,
        };
        var message = new RoleDialogModel(AgentRole.User, text)
        {
            CurrentAgentId = agentId,
            MessageId = messageId
        };

        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, new List<RoleDialogModel> { message });
        }
        
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);
        var options = PrepareOptions();
        var response = chatClient.CompleteChat([ new UserChatMessage(text) ], options);

        // AI response
        var content = response.Value?.Content ?? [];
        var completion = string.Empty;
        foreach (var t in content)
        {
            completion += t?.Text ?? string.Empty;
        };

        // After chat completion hook
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, completion)
        {
            CurrentAgentId = agentId,
            MessageId = messageId
        };

        var tokenUsage = response?.Value?.Usage;
        var inputTokenDetails = response?.Value?.Usage?.InputTokenDetails;

        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = text,
                Provider = Provider,
                Model = _model,
                TextInputTokens = (tokenUsage?.InputTokenCount ?? 0) - (inputTokenDetails?.CachedTokenCount ?? 0),
                CachedTextInputTokens = inputTokenDetails?.CachedTokenCount ?? 0,
                TextOutputTokens = tokenUsage?.OutputTokenCount ?? 0
            });
        }

        return completion.Trim();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private ChatCompletionOptions PrepareOptions()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var maxTokens = int.Parse(state.GetState("max_tokens", "1024"));

        return new ChatCompletionOptions
        {
            Temperature = temperature,
            MaxOutputTokenCount = maxTokens
        };
    }
}
