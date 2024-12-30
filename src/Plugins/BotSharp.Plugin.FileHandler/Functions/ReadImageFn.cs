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
        var agentService = _services.GetRequiredService<IAgentService>();

        var wholeDialogs = conv.GetDialogHistory();
        var dialogs = AssembleFiles(conv.ConversationId, args?.ImageUrls, wholeDialogs);
        var agent = new Agent
        {
            Id = BuiltInAgentId.UtilityAssistant,
            Name = "Utility Agent",
            Instruction = !string.IsNullOrWhiteSpace(args?.UserRequest) ? args.UserRequest : "Please describe the image(s).",
            TemplateDict = new Dictionary<string, object>()
        };

        if (!string.IsNullOrEmpty(message.CurrentAgentId))
        {
            agent = await agentService.LoadAgent(message.CurrentAgentId, loadUtility: false);
        }

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
            var lastDialog = dialogs.Last();
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
            var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
            var provider = llmProviderService.GetProviders().FirstOrDefault(x => x == "openai");
            var model = llmProviderService.GetProviderModel(provider: provider, id: "gpt-4", multiModal: true);
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model.Name);
            var response = await completion.GetChatCompletions(agent, dialogs);
            return response.Content;
        }
        catch (Exception ex)
        {
            var error = $"Error when analyzing images.";
            _logger.LogWarning($"{error} {ex.Message}\r\n{ex.InnerException}");
            return error;
        }
    }
}
