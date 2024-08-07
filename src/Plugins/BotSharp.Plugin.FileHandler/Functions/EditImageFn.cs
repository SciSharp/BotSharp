using System.IO;

namespace BotSharp.Plugin.FileHandler.Functions;

public class EditImageFn : IFunctionCallback
{
    public string Name => "edit_image";
    public string Indication => "Editing image";

    private readonly IServiceProvider _services;
    private readonly ILogger<EditImageFn> _logger;
    private string _conversationId;
    private string _messageId;

    public EditImageFn(
        IServiceProvider services,
        ILogger<EditImageFn> logger)
    {
        _services = services;
        _logger = logger;
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
        state.SetState("image_format", "bytes");
        state.SetState("image_count", "1");
    }

    private async Task<MessageFileModel?> SelectImage(string? description)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var selecteds = await fileInstruct.SelectMessageFiles(_conversationId, description: description, contentTypes: new List<string> { MediaTypeNames.Image.Png });
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
            var completion = CompletionProvider.GetImageCompletion(_services, provider: "openai", model: "dall-e-2");
            var text = !string.IsNullOrWhiteSpace(description) ? description : message.Content;
            var dialog = RoleDialogModel.From(message, AgentRole.User, text);
            var agent = new Agent
            {
                Id = BuiltInAgentId.UtilityAssistant,
                Name = "Utility Assistant"
            };

            using var stream = File.OpenRead(image.FileStorageUrl);
            var result = await completion.GetImageEdits(agent, dialog, stream, image.FileName ?? string.Empty);
            stream.Close();
            SaveGeneratedImage(result?.GeneratedImages?.FirstOrDefault());

            return $"Image \"{image.FileName}.{image.FileType}\" is successfylly editted.";
        }
        catch (Exception ex)
        {
            var error = $"Error when getting image edit response. {ex.Message}";
            _logger.LogWarning($"{error}\r\n{ex.InnerException}");
            return error;
        }
    }

    private void SaveGeneratedImage(ImageGeneration? image)
    {
        if (image == null) return;

        var files = new List<BotSharpFile>()
        {
            new BotSharpFile
            {
                FileName = $"{Guid.NewGuid()}.png",
                FileData = $"data:{MediaTypeNames.Image.Png};base64,{image.ImageData}"
            }
        };

        var fileService = _services.GetRequiredService<IFileBasicService>();
        fileService.SaveMessageFiles(_conversationId, _messageId, FileSourceType.Bot, files);
    }
}
