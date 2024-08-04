using OpenAI.Chat;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly AzureOpenAiSettings _settings;
    private string _model;
    public string Provider => "azure-openai";

    public TextCompletionProvider(IServiceProvider services,
        AzureOpenAiSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<string> GetCompletion(string text, string agentId, string messageId)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

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

        Task.WaitAll(contentHooks.Select(hook =>
            hook.BeforeGenerating(agent,
                new List<RoleDialogModel>
                {
                    message
                })).ToArray());

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);

        var messages = new List<ChatMessage>()
        {
            new UserChatMessage(text)
        };

        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var completionOptions = new ChatCompletionOptions()
        {
            MaxTokens = 256,
            Temperature = temperature
        };

        var response = await chatClient.CompleteChatAsync(messages, completionOptions);

        // OpenAI
        var completion = "";
        foreach (var t in response.Value.Content)
        {
            completion += t?.Text ?? string.Empty;
        };

        // After chat completion hook
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, completion)
        {
            CurrentAgentId = agentId,
            MessageId = messageId
        };
        Task.WaitAll(contentHooks.Select(hook =>
            hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = text,
                Provider = Provider,
                Model = _model,
                PromptCount = response.Value.Usage.InputTokens,
                CompletionCount = response.Value.Usage.OutputTokens
            })).ToArray());

        return completion.Trim();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
