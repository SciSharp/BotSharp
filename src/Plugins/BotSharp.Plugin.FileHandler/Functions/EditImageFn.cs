using BotSharp.Abstraction.Templating;
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

        var image = await SelectConversationImage();
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

    private async Task<MessageFileModel?> SelectConversationImage()
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var dialogs = convService.GetDialogHistory(fromBreakpoint: false);
        var messageIds = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var images = fileService.GetMessageFiles(_conversationId, messageIds, FileSourceType.User, imageOnly: true);
        return await SelectImage(images, dialogs);
    }

    private async Task<MessageFileModel?> SelectImage(IEnumerable<MessageFileModel> images, List<RoleDialogModel> dialogs)
    {
        if (images.IsNullOrEmpty()) return null;

        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        try
        {
            var promptImages = images.Where(x => x.ContentType == MediaTypeNames.Image.Png).Select((x, idx) =>
            {
                return $"id: {idx + 1}, image_name: {x.FileName}.{x.FileType}";
            }).ToList();
            
            if (promptImages.IsNullOrEmpty()) return null;

            var prompt = db.GetAgentTemplate(BuiltInAgentId.UtilityAssistant, "select_edit_image_prompt");
            prompt = render.Render(prompt, new Dictionary<string, object>
            {
                { "image_list", promptImages }
            });

            var agent = new Agent
            {
                Id = BuiltInAgentId.UtilityAssistant,
                Name = "Utility Assistant",
                Instruction = prompt
            };

            var provider = llmProviderService.GetProviders().FirstOrDefault(x => x == "openai");
            var model = llmProviderService.GetProviderModel(provider: provider, id: "gpt-4");
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model.Name);
            var response = await completion.GetChatCompletions(agent, dialogs);
            var content = response?.Content ?? string.Empty;
            var fid = JsonSerializer.Deserialize<int?>(content);
            return images.Where((x, idx) => idx == fid - 1).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting the image edit response. {ex.Message}\r\n{ex.InnerException}");
            return null;
        }
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

            return !string.IsNullOrWhiteSpace(result?.Content) ? result.Content : "Image edit is completed.";
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

        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        fileService.SaveMessageFiles(_conversationId, _messageId, FileSourceType.Bot, files);
    }
}
