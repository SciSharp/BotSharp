using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<IEnumerable<MessageFileModel>> SelectMessageFiles(string conversationId,
        string? agentId = null, string? template = null, bool includeBotFile = false, bool fromBreakpoint = false,
        int? offset = null, IEnumerable<string>? contentTypes = null)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return Enumerable.Empty<MessageFileModel>();
        }

        var convService = _services.GetRequiredService<IConversationService>();
        var dialogs = convService.GetDialogHistory(fromBreakpoint: fromBreakpoint);
        var messageIds = GetMessageIds(dialogs, offset);

        var files = _fileBasic.GetMessageFiles(conversationId, messageIds, FileSourceType.User, contentTypes);
        if  (includeBotFile)
        {
            var botFiles = _fileBasic.GetMessageFiles(conversationId, messageIds, FileSourceType.Bot, contentTypes);
            files = files.Concat(botFiles);
        }

        if (files.IsNullOrEmpty())
        {
            return Enumerable.Empty<MessageFileModel>();
        }

        return await SelectFiles(agentId, template, files, dialogs);
    }

    private async Task<IEnumerable<MessageFileModel>> SelectFiles(string? agentId, string? template, IEnumerable<MessageFileModel> files, List<RoleDialogModel> dialogs)
    {
        if (files.IsNullOrEmpty()) return new List<MessageFileModel>();

        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        try
        {
            var promptFiles = files.Select((x, idx) =>
            {
                return $"id: {idx + 1}, file_name: {x.FileName}.{x.FileType}, content_type: {x.ContentType}, author: {x.FileSource}";
            }).ToList();

            agentId = !string.IsNullOrWhiteSpace(agentId) ? agentId : BuiltInAgentId.UtilityAssistant;
            template = !string.IsNullOrWhiteSpace(template) ? template : "select_file_prompt";

            var foundAgent = db.GetAgent(agentId);
            var prompt = db.GetAgentTemplate(agentId, template);
            prompt = render.Render(prompt, new Dictionary<string, object>
            {
                { "file_list", promptFiles }
            });

            var agent = new Agent
            {
                Id = foundAgent?.Id ?? BuiltInAgentId.UtilityAssistant,
                Name = foundAgent?.Name ?? "Utility Assistant",
                Instruction = prompt
            };

            var provider = llmProviderService.GetProviders().FirstOrDefault(x => x == "openai");
            var model = llmProviderService.GetProviderModel(provider: provider, id: "gpt-4");
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model.Name);
            var latest = dialogs.Last();
            var response = await completion.GetChatCompletions(agent, new List<RoleDialogModel> { latest });
            var content = response?.Content ?? string.Empty;
            var selecteds = JsonSerializer.Deserialize<FileSelectContext>(content);
            var fids = selecteds?.Selecteds ?? new List<int>();
            return files.Where((x, idx) => fids.Contains(idx + 1)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when selecting files. {ex.Message}\r\n{ex.InnerException}");
            return new List<MessageFileModel>();
        }
    }

    private IEnumerable<string> GetMessageIds(IEnumerable<RoleDialogModel> conversations, int? offset = null)
    {
        if (conversations.IsNullOrEmpty()) return Enumerable.Empty<string>();

        if (offset.HasValue && offset < 1)
        {
            offset = 1;
        }

        var messageIds = new List<string>();
        if (offset.HasValue)
        {
            messageIds = conversations.Select(x => x.MessageId).Distinct().TakeLast(offset.Value).ToList();
        }
        else
        {
            messageIds = conversations.Select(x => x.MessageId).Distinct().ToList();
        }

        return messageIds;
    }
}
