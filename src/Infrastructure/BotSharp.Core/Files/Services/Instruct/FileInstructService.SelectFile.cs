using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Models;
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

        var routeContext = _services.GetRequiredService<IRoutingContext>();
        var convService = _services.GetRequiredService<IConversationService>();

        var dialogs = routeContext.GetDialogs();
        if (dialogs.IsNullOrEmpty())
        {
            dialogs = convService.GetDialogHistory(fromBreakpoint: options.FromBreakpoint);
        }

        if (options.MessageLimit > 0)
        {
            dialogs = dialogs.TakeLast(options.MessageLimit.Value).ToList();
        }

        var messageIds = dialogs.Select(x => x.MessageId).Distinct().ToList(); 
        var files = _fileStorage.GetMessageFiles(conversationId, messageIds, options: new()
        {
            Sources = options.IsIncludeBotFiles ?[FileSource.User, FileSource.Bot] : [FileSource.User],
            ContentTypes = options.ContentTypes
        });
        files = MergeMessageFiles(messageIds, files);

        if (files.IsNullOrEmpty())
        {
            return Enumerable.Empty<MessageFileModel>();
        }

        return await SelectFiles(files, dialogs, options);
    }

    private IEnumerable<MessageFileModel> MergeMessageFiles(IEnumerable<string> messageIds, IEnumerable<MessageFileModel> files)
    {
        var mergedFiles = new List<MessageFileModel>();

        if (messageIds.IsNullOrEmpty())
        {
            return mergedFiles;
        }

        var userFiles = files.Where(x => x.FileSource.IsEqualTo(FileSource.User));
        var botFiles = files.Where(x => x.FileSource.IsEqualTo(FileSource.Bot));

        foreach (var messageId in messageIds)
        {
            var users = userFiles.Where(x => x.MessageId == messageId).OrderBy(x => x.FileIndex, new MessageFileIndexComparer()).ToList();
            var bots = botFiles.Where(x => x.MessageId == messageId).OrderBy(x => x.FileIndex, new MessageFileIndexComparer()).ToList();
            
            if (!users.IsNullOrEmpty())
            {
                mergedFiles.AddRange(users);
            }
            if (!bots.IsNullOrEmpty())
            {
                mergedFiles.AddRange(bots);
            }
        }

        return files;
    }

    private async Task<IEnumerable<MessageFileModel>> SelectFiles(IEnumerable<MessageFileModel> files, IEnumerable<RoleDialogModel> dialogs, SelectFileOptions options)
    {
        var res = new List<MessageFileModel>();
        if (files.IsNullOrEmpty())
        {
            return res;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        // Handle dialogs and files
        var innerDialogs = (dialogs ?? []).ToList();
        var text = !string.IsNullOrWhiteSpace(options.Description) ? options.Description : "Please follow the instruction and select file(s).";
        innerDialogs = innerDialogs.Concat([new RoleDialogModel(AgentRole.User, text)]).ToList();

        if (options.IsAttachFiles)
        {
            AssembleMessageFiles(innerDialogs, files, options);
        }

        try
        {
            // Handle instruction
            var promptMessages = innerDialogs.Select(x =>
            {
                var text = $"[Role] '{x.Role}': {x.RichContent?.Message?.Text ?? x.Payload ?? x.Content}";
                var fileDescs = x.Files?.Select((f, fidx) => $"- message_id: '{x.MessageId}', file_index: '{f.FileIndex}', " +
                              $"content_type: '{f.ContentType}', author: '{(x.Role == AgentRole.User ? FileSource.User : FileSource.Bot)}'");

                var desc = string.Empty;
                if (!fileDescs.IsNullOrEmpty())
                {
                    desc = $"[Files]: \r\n\t{string.Join("\r\n\t", fileDescs)}";
                }

                return new NameDesc(text, desc);
            }).ToList();

            var agentId = !string.IsNullOrWhiteSpace(options.AgentId) ? options.AgentId : BuiltInAgentId.UtilityAssistant;
            var template = !string.IsNullOrWhiteSpace(options.Template) ? options.Template : "util-file-select_file_instruction";
            var prompt = db.GetAgentTemplate(agentId, template);

            var data = new Dictionary<string, object>
            {
                { "message_files", promptMessages }
            };

            if (!options.Data.IsNullOrEmpty())
            {
                foreach (var item in options.Data)
                {
                    data[item.Key] = item.Value;
                }
            }
            prompt = render.Render(prompt, data);


            // Build agent
            var foundAgent = await agentService.GetAgent(agentId);
            var agent = new Agent
            {
                Id = foundAgent?.Id ?? BuiltInAgentId.UtilityAssistant,
                Name = foundAgent?.Name ?? "Utility Assistant",
                Instruction = prompt,
                LlmConfig = new AgentLlmConfig
                {
                    MaxOutputTokens = options.MaxOutputTokens,
                    ReasoningEffortLevel = options.ReasoningEffortLevel
                }
            };


            // Get ai response
            var provider = options.LlmProvider ?? "openai";
            var model = options?.LlmModel ?? "gpt-5-mini";
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model);

            var response = await completion.GetChatCompletions(agent, innerDialogs);
            var content = response?.Content ?? "{}";
            var selecteds = JsonSerializer.Deserialize<FileSelectContext>(content, new JsonSerializerOptions
            {
                AllowTrailingCommas = true
            });
            var selectedFiles = selecteds?.SelectedFiles ?? new List<FileSelectItem>();
            
            if (!selectedFiles.IsNullOrEmpty())
            {
                res = files.Where(file => selectedFiles.Any(x => x.MessageId.IsEqualTo(file.MessageId)
                                                           && x.FileIndex.IsEqualTo(file.FileIndex)
                                                           && x.FileSource.IsEqualTo(file.FileSource))).ToList();
            }
            
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when selecting files.");
            return [];
        }
        finally
        {
            innerDialogs.ForEach(x => x.Files = null);
        }
    }

    private void AssembleMessageFiles(IEnumerable<RoleDialogModel> dialogs, IEnumerable<MessageFileModel> files, SelectFileOptions options)
    {
        if (dialogs.IsNullOrEmpty() || files.IsNullOrEmpty())
        {
            return;
        }

        var groupedDialogs = dialogs.GroupBy(x => x.MessageId);
        foreach (var group in groupedDialogs)
        {
            var targetMessageId = group.Key;
            var found = files.Where(x => x.MessageId == targetMessageId);

            if (found.IsNullOrEmpty())
            {
                continue;
            }

            var userMsg = group.FirstOrDefault(x => x.IsFromUser);
            if (userMsg != null)
            {
                var userFiles = found.Where(x => x.FileSource == FileSource.User);
                userMsg.Files = userFiles.Select(x => new BotSharpFile
                {
                    ContentType = x.ContentType,
                    FileUrl = x.FileUrl,
                    FileStorageUrl = x.FileStorageUrl,
                    FileName = x.FileName,
                    FileExtension = x.FileExtension,
                    FileIndex = x.FileIndex
                }).ToList();
            }

            var botMsg = group.LastOrDefault(x => x.IsFromAssistant);
            if (botMsg != null)
            {
                var botFiles = found.Where(x => x.FileSource == FileSource.Bot);
                botMsg.Files = botFiles.Select(x => new BotSharpFile
                {
                    ContentType = x.ContentType,
                    FileUrl = x.FileUrl,
                    FileStorageUrl = x.FileStorageUrl,
                    FileName = x.FileName,
                    FileExtension = x.FileExtension,
                    FileIndex = x.FileIndex
                }).ToList();
            }
        }
    }

    private sealed class MessageFileIndexComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == null) return -1;
            if (y == null) return 1;

            var isNumx = int.TryParse(x, out var xNum);
            var isNumy = int.TryParse(y, out var yNum);

            if (isNumx && isNumy)
            {
                return xNum.CompareTo(yNum);
            }

            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }
    }
}