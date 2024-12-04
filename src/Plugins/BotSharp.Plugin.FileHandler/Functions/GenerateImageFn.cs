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
        var agent = await agentService.LoadAgent(BuiltInAgentId.UtilityAssistant);
        var imageAgent = new Agent
        {
            Id = agent?.Id ?? Guid.Empty.ToString(),
            Name = agent?.Name ?? "Unkown",
            Instruction = args?.ImageDescription,
            TemplateDict = new Dictionary<string, object>()
        };

        var response = await GetImageGeneration(imageAgent, message, args?.ImageDescription);
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
        state.SetState("image_response_format", "bytes");
        state.SetState("image_count", "1");
    }

    private async Task<string> GetImageGeneration(Agent agent, RoleDialogModel message, string? description)
    {
        try
        {
            var completion = CompletionProvider.GetImageCompletion(_services, provider: "openai", model: "dall-e-3");
            var text = !string.IsNullOrWhiteSpace(description) ? description : message.Content;
            var dialog = RoleDialogModel.From(message, AgentRole.User, text);
            var result = await completion.GetImageGeneration(agent, dialog);
            SaveGeneratedImages(result?.GeneratedImages);
            return result?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            var error = $"Error when generating image.";
            _logger.LogWarning($"{error} {ex.Message}\r\n{ex.InnerException}");
            return error;
        }
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
