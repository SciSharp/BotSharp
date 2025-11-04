using BotSharp.Abstraction.Conversations.Settings;

namespace BotSharp.Plugin.ImageHandler.Functions;

public class ComposeImageFn : IFunctionCallback
{
    public string Name => "util-image-compose_images";
    public string Indication => "Composing images";

    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly ImageHandlerSettings _settings;

    private string _conversationId;
    private string _messageId;

    public ComposeImageFn(
        IServiceProvider services,
        ILogger<EditImageFn> logger,
        ImageHandlerSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);
        var descrpition = args?.UserRequest ?? string.Empty;

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(message.CurrentAgentId);

        Init(message);
        SetImageOptions();

        var image = await SelectImage(descrpition);
        var response = await GetImageEditGeneration(agent, message, descrpition, image);
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
        state.SetState("image_response_format", "bytes");
    }

    private async Task<MessageFileModel?> SelectImage(string? description)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var convSettings = _services.GetRequiredService<ConversationSetting>();

        var selecteds = await fileInstruct.SelectMessageFiles(_conversationId, new()
        {
            Description = description,
            IsIncludeBotFiles = true,
            IsAttachFiles = true,
            ContentTypes = [MediaTypeNames.Image.Png, MediaTypeNames.Image.Jpeg],
            MessageLimit = convSettings?.FileSelect?.MessageLimit,
            MaxOutputTokens = convSettings?.FileSelect?.MaxOutputTokens,
            ReasoningEffortLevel = convSettings?.FileSelect?.ReasoningEffortLevel
        });
        return selecteds?.FirstOrDefault();
    }

    private async Task<string> GetImageEditGeneration(Agent agent, RoleDialogModel message, string description, MessageFileModel? image)
    {
        if (image == null)
        {
            return "Failed to find an image. Please provide an image.";
        }

        try
        {
            var (provider, model) = GetLlmProviderModel(agent);
            var completion = CompletionProvider.GetImageCompletion(_services, provider: provider, model: model);
            var text = !string.IsNullOrWhiteSpace(description) ? description : message.Content;
            var dialog = RoleDialogModel.From(message, AgentRole.User, text);

            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var fileBinary = fileStorage.GetFileBytes(image.FileStorageUrl);
            var rgbaBinary = await ConvertImageToPngWithRgba(fileBinary);
            image.FileExtension = "png";

            using var stream = rgbaBinary.ToStream();
            stream.Position = 0;
            var response = await completion.GetImageEdits(agent, dialog, stream, image.FileFullName);
            stream.Close();

            var savedFiles = SaveGeneratedImage(response?.GeneratedImages?.FirstOrDefault());

            if (!string.IsNullOrWhiteSpace(response?.Content))
            {
                return response.Content;
            }

            return await AiResponseHelper.GetImageGenerationResponse(_services, agent, description, savedFiles);
        }
        catch (Exception ex)
        {
            var error = $"Error when getting image edit response. {ex.Message}";
            _logger.LogWarning(ex, $"{error}");
            return error;
        }
    }

    private (string, string) GetLlmProviderModel(Agent agent)
    {
        var provider = agent?.LlmConfig?.ImageComposition?.Provider;
        var model = agent?.LlmConfig?.ImageComposition?.Model;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = _settings?.Composition?.Provider;
        model = _settings?.Composition?.Model;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-image-1-mini";

        return (provider, model);
    }

    private IEnumerable<string> SaveGeneratedImage(ImageGeneration? image)
    {
        if (image == null)
        {
            return [];
        }

        var files = new List<FileDataModel>()
        {
            new FileDataModel
            {
                FileName = $"{Guid.NewGuid()}.png",
                FileData = $"data:{MediaTypeNames.Image.Png};base64,{image.ImageData}"
            }
        };

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        fileStorage.SaveMessageFiles(_conversationId, _messageId, FileSource.Bot, files);
        return files.Select(x => x.FileName);
    }

    private async Task<BinaryData> ConvertImageToPngWithRgba(BinaryData binaryFile)
    {
        var provider = _settings?.Composition?.ImageConverter?.Provider;
        var converter = _services.GetServices<IImageConverter>().FirstOrDefault(x => x.Provider == provider);
        if (converter == null)
        {
            return binaryFile;
        }

        return await converter.ConvertImage(binaryFile);
    }
}
