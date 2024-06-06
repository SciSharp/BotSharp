using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Files.Functions;

public class LoadAttachmentFn : IFunctionCallback
{
    public string Name => "load_attachment";
    public string Indication => "Analyzing files";

    private readonly IServiceProvider _services;
    private readonly ILogger<LoadAttachmentFn> _logger;
    private const string AIAssistant = "01fcc3e5-9af7-49e6-ad7a-a760bd12dc4a";

    public LoadAttachmentFn(
        IServiceProvider services,
        ILogger<LoadAttachmentFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        
        var wholeDialogs = conv.GetDialogHistory();
        var dialogs = AssembleFiles(conv.ConversationId, wholeDialogs);
        var agent = await agentService.LoadAgent(AIAssistant);
        var fileAgent = new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Instruction = "Please describe the images.",
            TemplateDict = new Dictionary<string, object>()
        };

        var response = await GetChatCompletion(fileAgent, dialogs);
        message.Content = response;
        message.StopCompletion = true;
        return true;
    }

    private List<RoleDialogModel> AssembleFiles(string conversationId, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty())
        {
            return new List<RoleDialogModel>();
        }

        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var files = fileService.GetChatImages(conversationId, FileSourceType.User, dialogs);

        foreach (var dialog in dialogs)
        {
            var found = files.Where(x => x.MessageId == dialog.MessageId).ToList();
            if (found.IsNullOrEmpty()) continue;

            dialog.Files = found.Select(x => new BotSharpFile
            {
                FileName = x.FileName,
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
            var error = $"Error when analyzing files.";
            _logger.LogWarning($"{error} {ex.Message}");
            return error;
        }
    }
}
