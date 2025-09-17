using BotSharp.Abstraction.Conversations.Settings;

namespace BotSharp.Plugin.FileHandler.Functions;

public class EditImageFn : IFunctionCallback
{
    public string Name => "util-file-edit_image";
    public string Indication => "Editing image";

    private readonly IServiceProvider _services;
    private readonly ILogger<EditImageFn> _logger;
    private readonly FileHandlerSettings _settings;

    private Agent _agent;
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
        await Init(message);
        SetImageOptions();

        var image = await SelectImage(descrpition);
        var response = await GetImageEditGeneration(message, descrpition, image);
        message.Content = response;
        message.StopCompletion = true;
        return true;
    }

    private async Task Init(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var convService = _services.GetRequiredService<IConversationService>();

        _agent = await agentService.GetAgent(message.CurrentAgentId);
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

        var selecteds = await fileInstruct.SelectMessageFiles(_conversationId, new SelectFileOptions
        {
            Description = description,
            IsIncludeBotFiles = true,
            IsAttachFiles = true,
            ContentTypes = [MediaTypeNames.Image.Png, MediaTypeNames.Image.Jpeg],
            MessageLimit = convSettings?.FileSelect?.MessageLimit,
            LlmProvider = convSettings?.FileSelect?.LlmProvider,
            LlmModel = convSettings?.FileSelect?.LlmModel,
            MaxOutputTokens = convSettings?.FileSelect?.MaxOutputTokens,
            ReasoningEffortLevel = convSettings?.FileSelect?.ReasoningEffortLevel
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
                Id = _agent?.Id ?? BuiltInAgentId.UtilityAssistant,
                Name = _agent?.Name ?? "Utility Assistant"
            };

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

            return await GetImageEditResponse(description, defaultContent: null);
        }
        catch (Exception ex)
        {
            var error = $"Error when getting image edit response. {ex.Message}";
            _logger.LogWarning(ex, $"{error}");
            return error;
        }
    }

    private async Task<string> GetImageEditResponse(string description, string? defaultContent)
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

        var provider = state.GetState("image_edit_llm_provider");
        var model = state.GetState("image_edit_llm_provider");

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = _settings?.Image?.Edit?.LlmProvider;
        model = _settings?.Image?.Edit?.LlmModel;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-image-1";

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
        var provider = _settings?.Image?.Edit?.ImageConverter?.Provider;
        var converter = _services.GetServices<IImageConverter>().FirstOrDefault(x => x.Provider == provider);
        if (converter == null)
        {
            return binaryFile;
        }

        return await converter.ConvertImage(binaryFile);
    }
}
