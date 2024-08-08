using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<IEnumerable<MessageFileModel>> SelectMessageFiles(string conversationId, SelectFileOptions options)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return Enumerable.Empty<MessageFileModel>();
        }

        var convService = _services.GetRequiredService<IConversationService>();
        var dialogs = convService.GetDialogHistory(fromBreakpoint: options.FromBreakpoint);
        var messageIds = GetMessageIds(dialogs, options.Offset);

        var files = _fileStorage.GetMessageFiles(conversationId, messageIds, FileSourceType.User, options.ContentTypes);
        if  (options.IncludeBotFile)
        {
            var botFiles = _fileStorage.GetMessageFiles(conversationId, messageIds, FileSourceType.Bot, options.ContentTypes);
            files = files.Concat(botFiles);
        }

        if (files.IsNullOrEmpty())
        {
            return Enumerable.Empty<MessageFileModel>();
        }

        return await SelectFiles(files, dialogs, options);
    }

    private async Task<IEnumerable<MessageFileModel>> SelectFiles(IEnumerable<MessageFileModel> files, IEnumerable<RoleDialogModel> dialogs, SelectFileOptions options)
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

            var agentId = !string.IsNullOrWhiteSpace(options.AgentId) ? options.AgentId : BuiltInAgentId.UtilityAssistant;
            var template = !string.IsNullOrWhiteSpace(options.Template) ? options.Template : "select_file_prompt";

            var foundAgent = db.GetAgent(agentId);
            var prompt = db.GetAgentTemplate(BuiltInAgentId.UtilityAssistant, template);
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

            var message = dialogs.LastOrDefault();
            var text = !string.IsNullOrWhiteSpace(options.Description) ? options.Description : message?.Content;
            if (message == null)
            {
                message = new RoleDialogModel(AgentRole.User, text);
            }
            else
            {
                message = RoleDialogModel.From(message, AgentRole.User, text);
            }

            var providerName = options.Provider ?? "openai";
            var modelId = options?.ModelId ?? "gpt-4";
            var provider = llmProviderService.GetProviders().FirstOrDefault(x => x == providerName);
            var model = llmProviderService.GetProviderModel(provider: provider, id: modelId);
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model.Name);

            var response = await completion.GetChatCompletions(agent, new List<RoleDialogModel> { message });
            var content = response?.Content ?? string.Empty;
            var selecteds = JsonSerializer.Deserialize<FileSelectContext>(content, new JsonSerializerOptions
            {
                AllowTrailingCommas = true
            });
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
