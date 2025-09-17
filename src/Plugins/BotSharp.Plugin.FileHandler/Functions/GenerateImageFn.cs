namespace BotSharp.Plugin.FileHandler.Functions;

public class GenerateImageFn : IFunctionCallback
{
    public string Name => "util-file-generate_image";
    public string Indication => "Generating image";

    private readonly IServiceProvider _services;
    private readonly ILogger<GenerateImageFn> _logger;
    private readonly FileHandlerSettings _settings;

    private Agent _agent;
    private string _conversationId;
    private string _messageId;

    public GenerateImageFn(
        IServiceProvider services,
        ILogger<GenerateImageFn> logger,
        FileHandlerSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);
        Init(message);
        SetImageOptions();
        
        var agentService = _services.GetRequiredService<IAgentService>();
        _agent = await agentService.GetAgent(message.CurrentAgentId);

        var response = await GetImageGeneration(message, args?.ImageDescription);
        message.Content = response;
        message.StopCompletion = true;
        return true;
    }

    private void Init(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        _conversationId = convService.ConversationId;
        _messageId = message.MessageId;
    }

    private void SetImageOptions()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        state.SetState("image_count", "1");
        state.SetState("image_quality", "medium");
        state.SetState("image_response_format", "bytes");
    }

    private async Task<string> GetImageGeneration(RoleDialogModel message, string? description)
    {
        try
        {
            var agent = new Agent
            {
                Id = _agent?.Id ?? BuiltInAgentId.UtilityAssistant,
                Name = _agent?.Name ?? "Utility Assistant",
                Instruction = description
            };

            var (provider, model) = GetLlmProviderModel();
            var completion = CompletionProvider.GetImageCompletion(_services, provider: provider, model: model);
            var text = !string.IsNullOrWhiteSpace(description) ? description : message.Content;
            var dialog = RoleDialogModel.From(message, AgentRole.User, text);
            var result = await completion.GetImageGeneration(agent, dialog);
            var savedFiles = SaveGeneratedImages(result?.GeneratedImages);

            if (!string.IsNullOrWhiteSpace(result?.Content))
            {
                return result.Content;
            }

            return await GetImageGenerationResponse(description, defaultContent: null);
        }
        catch (Exception ex)
        {
            var error = $"Error when generating image.";
            _logger.LogWarning(ex, $"{error}");
            return error;
        }
    }

    private async Task<string> GetImageGenerationResponse(string description, string? defaultContent)
    {
        if (defaultContent != null)
        {
            return defaultContent;
        }

        var llmConfig = _agent.LlmConfig;
        var agent = new Agent
        {
            Id = _agent?.Id ?? BuiltInAgentId.UtilityAssistant,
            Name = _agent?.Name ?? "Utility Assistant",
            LlmConfig = new AgentLlmConfig
            {
                Provider = llmConfig?.Provider ?? "openai",
                Model = llmConfig?.Model ?? "gpt-4o-mini",
                MaxOutputTokens = llmConfig?.MaxOutputTokens,
                ReasoningEffortLevel = llmConfig?.ReasoningEffortLevel
            }
        };

        return await AiResponseHelper.GetImageGenerationResponse(_services, agent, description);
    }

    private (string, string) GetLlmProviderModel()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();

        var provider = state.GetState("image_generate_llm_provider");
        var model = state.GetState("image_generate_llm_model");

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = _settings?.Image?.Generation?.LlmProvider;
        model = _settings?.Image?.Generation?.LlmModel;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-image-1";

        return (provider, model);
    }

    private IEnumerable<string> SaveGeneratedImages(List<ImageGeneration>? images)
    {
        if (images.IsNullOrEmpty())
        {
            return [];
        }

        var files = images.Where(x => !string.IsNullOrEmpty(x?.ImageData)).Select(x => new FileDataModel
        {
            FileName = $"{Guid.NewGuid()}.png",
            FileData = $"data:{MediaTypeNames.Image.Png};base64,{x.ImageData}"
        }).ToList();

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        fileStorage.SaveMessageFiles(_conversationId, _messageId, FileSource.Bot, files);
        return files.Select(x => x.FileName);
    }
}
