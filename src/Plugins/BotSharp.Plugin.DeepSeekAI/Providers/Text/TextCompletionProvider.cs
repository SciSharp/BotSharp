using BotSharp.Abstraction.Hooks;
using Microsoft.Extensions.Logging;
using DeepSeek.Core;
using DeepSeek.Core.Models;
using System.Threading;

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
        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var maxTokens = int.Parse(state.GetState("max_tokens", "1024"));

        var request = new ChatRequest
        {
            Model = string.IsNullOrWhiteSpace(_model) ? DeepSeekModels.ChatModel : _model,
            Messages = new List<Message> { Message.NewUserMessage(text) },
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var response = await client.ChatAsync(request, CancellationToken.None);

        var completion = ExtractText(response);

        var responseMessage = new RoleDialogModel(AgentRole.Assistant, completion)
        {
            CurrentAgentId = agentId,
            MessageId = messageId
        };

        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = text,
                Provider = Provider,
                Model = _model
            });
        }

        return completion.Trim();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private string ExtractText(ChatResponse? response)
    {
        if (response?.Choices?.Count > 0)
        {
            var msg = response.Choices[0].Message;
            if (!string.IsNullOrEmpty(msg?.Content))
            {
                return msg.Content;
            }
        }
        return string.Empty;
    }
}
