namespace BotSharp.Plugin.FileHandler.Functions;

public class EditImageFn : IFunctionCallback
{
    public string Name => "util-file-edit_image";
    public string Indication => "Editing image";

    private readonly IServiceProvider _services;
    private readonly ILogger<EditImageFn> _logger;
    private readonly FileHandlerSettings _settings;

    private string _agentId;
    private string _conversationId;
    private string _messageId;

    public EditImageFn(
        IServiceProvider services,
        ILogger<EditImageFn> logger,
        FileHandlerSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);
        var descrpition = args?.UserRequest ?? string.Empty;
        Init(message);
        SetImageOptions();

        var image = await SelectImage(descrpition);
        var response = await GetImageEditGeneration(message, descrpition, image);
        message.Content = response;
        message.StopCompletion = true;
        return true;
    }

    private void Init(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        _agentId = message.CurrentAgentId;
        _conversationId = convService.ConversationId;
        _messageId = message.MessageId;
    }

    private void SetImageOptions()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        state.SetState("image_count", "1");
        state.SetState("image_response_format", "bytes");
    }

    private async Task<MessageFileModel?> SelectImage(string? description)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var selecteds = await fileInstruct.SelectMessageFiles(_conversationId, new SelectFileOptions
        {
            Description = description,
            IsIncludeBotFiles = true,
            IsAttachFiles = true,
            ContentTypes = [MediaTypeNames.Image.Png, MediaTypeNames.Image.Jpeg],
            MessageLimit = 50,
            Provider = "openai",
            Model = "gpt-5-mini",
            MaxOutputTokens = 8192,
            ReasoningEffortLevel = "low"
        });
        return selecteds?.FirstOrDefault();
    }

    private async Task<string> GetImageEditGeneration(RoleDialogModel message, string description, MessageFileModel? image)
    {
        if (image == null)
        {
            return "Failed to find an image. Please provide an image.";
        }

        try
        {
            var (provider, model) = GetLlmProviderModel();
            var completion = CompletionProvider.GetImageCompletion(_services, provider: provider, model: model);
            var text = !string.IsNullOrWhiteSpace(description) ? description : message.Content;
            var dialog = RoleDialogModel.From(message, AgentRole.User, text);
            var agent = new Agent
            {
                Id = BuiltInAgentId.UtilityAssistant,
                Name = "Utility Assistant"
            };

            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var fileBinary = fileStorage.GetFileBytes(image.FileStorageUrl);
            var rgbaBinary = await ConvertImageToPngWithRgba(fileBinary);
            image.FileExtension = "png";

            using var stream = rgbaBinary.ToStream();
            stream.Position = 0;
            var response = await completion.GetImageEdits(agent, dialog, stream, image.FileFullName);
            stream.Close();
            SaveGeneratedImage(response?.GeneratedImages?.FirstOrDefault());

            if (!string.IsNullOrWhiteSpace(response?.Content))
            {
                return response.Content;
            }

            return await GetImageEditGreetingResponse(description);
        }
        catch (Exception ex)
        {
            var error = $"Error when getting image edit response. {ex.Message}";
            _logger.LogWarning(ex, $"{error}");
            return error;
        }
    }

    private async Task<string> GetImageEditGreetingResponse(string description)
    {
        var agent = new Agent
        {
            Id = BuiltInAgentId.UtilityAssistant,
            Name = "Utility Assistant"
        };

        var text = $"Please generate a user-friendly response from the following description to inform user that you have completed the required image: {description}";

        var completion = CompletionProvider.GetChatCompletion(_services, provider: "openai", model: "gpt-4o-mini");
        var response = await completion.GetChatCompletions(agent, [new RoleDialogModel(AgentRole.User, text)]);
        return response?.Content ?? "Your image is successfully edited.";
    }

    private (string, string) GetLlmProviderModel()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var fileSettings = _services.GetRequiredService<FileHandlerSettings>();

        var provider = state.GetState("image_edit_llm_provider");
        var model = state.GetState("image_edit_llm_provider");

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = fileSettings?.Image?.Edit?.LlmProvider;
        model = fileSettings?.Image?.Edit?.LlmModel;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-image-1";

        return (provider, model);
    }

    private void SaveGeneratedImage(ImageGeneration? image)
    {
        if (image == null) return;

        var files = new List<FileDataModel>()
        {
            new FileDataModel
            {
                FileName = $"{Guid.NewGuid()}.png",
                FileData = $"data:{MediaTypeNames.Image.Png};base64,{image.ImageData}"
            }
        };

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        fileStorage.SaveMessageFiles(_conversationId, _messageId, FileSourceType.Bot, files);
    }

    private async Task<BinaryData> ConvertImageToPngWithRgba(BinaryData binaryFile)
    {
        var provider = _settings?.ImageConverter?.Provider;
        var converter = _services.GetServices<IImageConverter>().FirstOrDefault(x => x.Provider == provider);
        if (converter == null)
        {
            return binaryFile;
        }

        return await converter.ConvertImage(binaryFile);
    }
}
