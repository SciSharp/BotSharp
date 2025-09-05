using BotSharp.Abstraction.Routing;

namespace BotSharp.Plugin.FileHandler.Functions;

public class ReadImageFn : IFunctionCallback
{
    public string Name => "util-file-read_image";
    public string Indication => "Reading images";

    private readonly IServiceProvider _services;
    private readonly ILogger<ReadImageFn> _logger;

    public ReadImageFn(
        IServiceProvider services,
        ILogger<ReadImageFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs);
        var conv = _services.GetRequiredService<IConversationService>();
        var routingCtx = _services.GetRequiredService<IRoutingContext>();
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
        var images = fileStorage.GetMessageFiles(conversationId, messageIds, FileSourceType.User, new List<string>
        {
            MediaTypeNames.Image.Png,
            MediaTypeNames.Image.Jpeg
        });

        foreach (var dialog in dialogs)
        {
            var found = images.Where(x => x.MessageId == dialog.MessageId).ToList();
            if (found.IsNullOrEmpty()) continue;

            dialog.Files = found.Select(x => new BotSharpFile
            {
                ContentType = x.ContentType,
                FileUrl = x.FileUrl,
                FileStorageUrl = x.FileStorageUrl
            }).ToList();
        }

        if (!imageUrls.IsNullOrEmpty())
        {
            var lastDialog = dialogs.LastOrDefault(x => x.Role == AgentRole.User) ?? dialogs.Last();
            var files = lastDialog.Files ?? [];
            var addnFiles = imageUrls.Select(x => x?.Trim())
                                     .Where(x => !string.IsNullOrWhiteSpace(x))
                                     .Select(x => new BotSharpFile { FileUrl = x }).ToList();
            
            files.AddRange(addnFiles);
            lastDialog.Files = files;
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
        var state = _services.GetRequiredService<IConversationStateService>();
        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var fileSettings = _services.GetRequiredService<FileHandlerSettings>();

        var provider = state.GetState("image_read_llm_provider");
        var model = state.GetState("image_read_llm_model");

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = fileSettings?.Image?.Reading?.LlmProvider;
        model = fileSettings?.Image?.Reading?.LlmModel;

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
        var fileSettings = _services.GetRequiredService<FileHandlerSettings>();

        var key = "chat_image_detail_level";
        var level = state.GetState(key);

        if (string.IsNullOrWhiteSpace(level) && !string.IsNullOrWhiteSpace(fileSettings.Image?.Reading?.ImageDetailLevel))
        {
            state.SetState(key, fileSettings.Image.Reading.ImageDetailLevel);
        }
    }
}
