namespace BotSharp.Plugin.OpenAI.Providers.Chat;

public partial class ChatCompletionProvider : IChatCompletion
{
    protected readonly OpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<ChatCompletionProvider> _logger;
    protected readonly IConversationStateService _state;

    protected string _model;
    protected string? _apiKey;

    private List<string> renderedInstructions = [];

    public virtual string Provider => "openai";
    public string Model => _model;

    public ChatCompletionProvider(
        OpenAiSettings settings,
        ILogger<ChatCompletionProvider> logger,
        IServiceProvider services,
        IConversationStateService state)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
        _state = state;
    }


    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        if (_settings.UseResponseApi)
        {
            return await InnerGetResponse(agent, conversations);
        }
        else
        {
            return await InnerGetChatCompletions(agent, conversations);
        }
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent,
        List<RoleDialogModel> conversations,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        if (_settings.UseResponseApi)
        {
            return await InnerGetResponseAsync(agent, conversations, onMessageReceived, onFunctionExecuting);
        }
        else
        {
            return await InnerGetChatCompletionsAsync(agent, conversations, onMessageReceived, onFunctionExecuting);
        }
    }

    public async Task<RoleDialogModel> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        if (_settings.UseResponseApi)
        {
            return await InnerGetResponseStreamingAsync(agent, conversations);
        }
        else
        {
            return await InnerGetChatCompletionsStreamingAsync(agent, conversations);
        }
    }


    public void SetModelName(string model)
    {
        _model = model;
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    private static bool IsImageContentType(string? contentType)
    {
        return !string.IsNullOrEmpty(contentType)
            && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}