namespace BotSharp.Plugin.FileHandler.Functions;

public class GenerateImageFn : IFunctionCallback
{
    public string Name => "util-file-generate_image";
    public string Indication => "Generating image";

    private readonly IServiceProvider _services;
    private readonly ILogger<GenerateImageFn> _logger;
    private string _conversationId;
    private string _messageId;

    public GenerateImageFn(
        IServiceProvider services,
        ILogger<GenerateImageFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);
        Init(message);
        SetImageOptions();
        
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
            Instruction = args?.ImageDescription,
            TemplateDict = new Dictionary<string, object>()
        };

        var response = await GetImageGeneration(agent, message, args?.ImageDescription);
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
    }

    private async Task<string> GetImageGeneration(Agent agent, RoleDialogModel message, string? description)
    {
        try
        {
            var (provider, model) = GetLlmProviderModel();
            var completion = CompletionProvider.GetImageCompletion(_services, provider: provider, model: model);
            var text = !string.IsNullOrWhiteSpace(description) ? description : message.Content;
            var dialog = RoleDialogModel.From(message, AgentRole.User, text);
            var result = await completion.GetImageGeneration(agent, dialog);
            SaveGeneratedImages(result?.GeneratedImages);
            return result?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            var error = $"Error when generating image.";
            _logger.LogWarning(ex, $"{error}");
            return error;
        }
    }

    private (string, string) GetLlmProviderModel()
    {
        var provider = "openai";
        var model = "gpt-image-1";

        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var models = llmProviderService.GetProviderModels(provider);
        var foundModel = models.FirstOrDefault(x => x.Image?.Generation?.IsDefault == true)
                            ?? models.FirstOrDefault(x => x.Image?.Generation != null);

        model = foundModel?.Name ?? model;
        return (provider, model);
    }

    private void SaveGeneratedImages(List<ImageGeneration>? images)
    {
        if (images.IsNullOrEmpty()) return;

        var files = images.Where(x => !string.IsNullOrEmpty(x?.ImageData)).Select(x => new FileDataModel
        {
            FileName = $"{Guid.NewGuid()}.png",
            FileData = $"data:{MediaTypeNames.Image.Png};base64,{x.ImageData}"
        }).ToList();

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        fileStorage.SaveMessageFiles(_conversationId, _messageId, FileSourceType.Bot, files);
    }
}
