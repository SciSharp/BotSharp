namespace BotSharp.Plugin.FileHandler.Functions;

public class ReadImageFn : IFunctionCallback
{
    public string Name => "read_image";
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
        var dialogs = await AssembleFiles(conv.ConversationId, wholeDialogs);
        var agent = await agentService.LoadAgent(BuiltInAgentId.UtilityAssistant);
        var fileAgent = new Agent
        {
            Id = agent?.Id ?? Guid.Empty.ToString(),
            Name = agent?.Name ?? "Unkown",
            Instruction = !string.IsNullOrWhiteSpace(args?.UserRequest) ? args.UserRequest : "Please describe the image(s).",
            TemplateDict = new Dictionary<string, object>()
        };

        var response = await GetChatCompletion(fileAgent, dialogs);
        message.Content = response;
        return true;
    }

    private async Task<List<RoleDialogModel>> AssembleFiles(string conversationId, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty())
        {
            return new List<RoleDialogModel>();
        }

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();

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
