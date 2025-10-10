using BotSharp.Abstraction.Routing;

namespace BotSharp.Plugin.ImageHandler.Functions;

public class ReadImageFn : IFunctionCallback
{
    public string Name => "util-file-read_image";
    public string Indication => "Reading images";

    private readonly IServiceProvider _services;
    private readonly ILogger<ReadImageFn> _logger;
    private readonly ImageHandlerSettings _settings;

    private readonly IEnumerable<string> _imageContentTypes = new List<string>
    {
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Jpeg
    };

    public ReadImageFn(
        IServiceProvider services,
        ILogger<ReadImageFn> logger,
        ImageHandlerSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);
        var agentService = _services.GetRequiredService<IAgentService>();
        var conv = _services.GetRequiredService<IConversationService>();
        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        
        var fromAgent = await agentService.GetAgent(message.CurrentAgentId);
        var agent = new Agent
        {
            Id = fromAgent?.Id ?? BuiltInAgentId.FileAssistant,
            Name = fromAgent?.Name ?? "File Assistant",
            Instruction = fromAgent?.Instruction ?? args?.UserRequest ?? "Please describe the image(s).",
            LlmConfig = fromAgent?.LlmConfig ?? new()
        };

        var wholeDialogs = routingCtx.GetDialogs();
        if (wholeDialogs.IsNullOrEmpty())
        {
            wholeDialogs = conv.GetDialogHistory();
        }

        var dialogs = AssembleFiles(conv.ConversationId, args?.ImageUrls, wholeDialogs);
        var response = await GetChatCompletion(agent, dialogs);
        dialogs.ForEach(x => x.Files = null);
        message.Content = response;
        return true;
    }

    private List<RoleDialogModel> AssembleFiles(string conversationId, IEnumerable<string>? imageUrls, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty())
        {
            return new List<RoleDialogModel>();
        }

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var messageIds = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var images = fileStorage.GetMessageFiles(conversationId, messageIds, options: new()
        {
            Sources = [FileSource.User, FileSource.Bot],
            ContentTypes = _imageContentTypes
        });

        foreach (var dialog in dialogs)
        {
            var found = images.Where(x => x.MessageId == dialog.MessageId).ToList();
            if (found.IsNullOrEmpty()) continue;

            var targets = found;
            if (dialog.IsFromUser)
            {
                targets = found.Where(x => x.FileSource.IsEqualTo(FileSource.User)).ToList();
            }
            else if (dialog.IsFromAssistant)
            {
                targets = found.Where(x => x.FileSource.IsEqualTo(FileSource.Bot)).ToList();
            }

            dialog.Files = targets.Select(x => new BotSharpFile
            {
                ContentType = x.ContentType,
                FileUrl = x.FileUrl,
                FileStorageUrl = x.FileStorageUrl
            }).ToList();
        }

        if (!imageUrls.IsNullOrEmpty())
        {
            var lastDialog = dialogs.LastOrDefault(x => x.Role == AgentRole.User) ?? dialogs.Last();
            lastDialog.Files ??= [];

            var addnFiles = imageUrls.Select(x => x?.Trim())
                                     .Where(x => !string.IsNullOrWhiteSpace(x))
                                     .Select(x => new BotSharpFile { FileUrl = x }).ToList();
            lastDialog.Files.AddRange(addnFiles);
        }

        return dialogs;
    }

    private async Task<string> GetChatCompletion(Agent agent, List<RoleDialogModel> dialogs)
    {
        try
        {
            var (provider, model) = GetLlmProviderModel();
            SetImageDetailLevel();
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model);
            var response = await completion.GetChatCompletions(agent, dialogs);
            return response.Content;
        }
        catch (Exception ex)
        {
            var error = $"Error when analyzing images.";
            _logger.LogWarning(ex, $"{error}");
            return error;
        }
    }

    private (string, string) GetLlmProviderModel()
    {
        var provider = _settings?.Reading?.Provider;
        var model = _settings?.Reading?.Model;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-5-mini";

        return (provider, model);
    }

    private void SetImageDetailLevel()
    {
        var state = _services.GetRequiredService<IConversationStateService>();

        var key = "chat_image_detail_level";
        var level = state.GetState(key);

        if (string.IsNullOrWhiteSpace(level) && !string.IsNullOrWhiteSpace(_settings.Reading?.ImageDetailLevel))
        {
            state.SetState(key, _settings.Reading.ImageDetailLevel);
        }
    }
}
