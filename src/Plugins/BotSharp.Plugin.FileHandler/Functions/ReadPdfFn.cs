namespace BotSharp.Plugin.FileHandler.Functions;

public class ReadPdfFn : IFunctionCallback
{
    public string Name => "read_pdf";
    public string Indication => "Reading pdf";

    private readonly IServiceProvider _services;
    private readonly ILogger<ReadPdfFn> _logger;

    private readonly IEnumerable<string> _pdfContentTypes = new List<string>
    {
        MediaTypeNames.Application.Pdf
    };

    public ReadPdfFn(
        IServiceProvider services,
        ILogger<ReadPdfFn> logger)
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
            Instruction = !string.IsNullOrWhiteSpace(args?.UserRequest) ? args.UserRequest : "Please describe the pdf file(s).",
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

        var fileService = _services.GetRequiredService<IFileBasicService>();
        var files = await fileService.GetChatFiles(conversationId, FileSourceType.User, dialogs, _pdfContentTypes, includeScreenShot: true);

        foreach (var dialog in dialogs)
        {
            var found = files.Where(x => x.MessageId == dialog.MessageId).ToList();
            if (found.IsNullOrEmpty()) continue;

            dialog.Files = found.Select(x => new BotSharpFile
            {
                ContentType = x.ContentType,
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
            var error = $"Error when analyzing pdf file(s).";
            _logger.LogWarning($"{error} {ex.Message}\r\n{ex.InnerException}");
            return error;
        }
    }
}
