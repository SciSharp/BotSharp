using System.Collections.Concurrent;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    // Conversations currently being compressed. Compression runs fire-and-forget after every turn,
    // and summarization is a slow LLM call, so multiple runs can overlap for the same conversation.
    // This guard lets only one run per conversation proceed; overlapping ones skip and are picked up
    // by the next turn's trigger once the in-flight run finishes.
    private static readonly ConcurrentDictionary<string, byte> _compressingConversations = new();

    public async Task<bool> AutoCompressIfNeeded(string conversationId)
    {
        var setting = _settings.AutoCompression;
        if (setting == null || !setting.Enabled || string.IsNullOrEmpty(conversationId))
        {
            return false;
        }

        // Skip if a compaction for this conversation is already running.
        if (!_compressingConversations.TryAdd(conversationId, 0))
        {
            return false;
        }

        try
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var conv = await db.GetConversation(conversationId);
            if (conv == null)
            {
                return false;
            }

            // Load the full stored history in chronological order.
            var dialogs = await _storage.GetDialogs(conversationId);
            if (dialogs.IsNullOrEmpty())
            {
                return false;
            }

            // Trigger is relative to the last breakpoint: only count the messages the LLM currently
            // sees. This keeps a single, stable threshold across both context-only and storage modes
            // and prevents re-firing on every turn once the total count is large.
            var lastBreakpoint = await db.GetConversationBreakpoint(conversationId);
            var activeCount = lastBreakpoint != null
                ? dialogs.Count(x => x.CreatedAt >= lastBreakpoint.Breakpoint)
                : dialogs.Count;

            if (activeCount < setting.TriggerMessageCount)
            {
                return false;
            }

            // Keep the most recent messages verbatim; summarize everything before the cut,
            // snapped to a safe turn boundary.
            var cutIndex = ComputeCompressionCut(dialogs, setting.KeepRecentCount);
            if (cutIndex <= 0)
            {
                return false;
            }

            var cutDialog = dialogs[cutIndex];
            var toSummarize = dialogs.Take(cutIndex).ToList();
            if (toSummarize.IsNullOrEmpty())
            {
                return false;
            }

            // Use the configured summary agent, or fall back to the conversation's own agent.
            var summaryAgentId = string.IsNullOrEmpty(setting.SummaryAgentId) ? conv.AgentId : setting.SummaryAgentId;
            var summary = await SummarizeForCompression(summaryAgentId, setting, toSummarize);
            if (string.IsNullOrWhiteSpace(summary))
            {
                _logger.LogWarning($"Auto-compression produced an empty summary for conversation {conversationId}; skipping.");
                return false;
            }

            // Context layer: a breakpoint at the cut so GetDialogHistory drops older raw turns and
            // injects the summary as a leading message. The last breakpoint always wins.
            var breakpoint = new ConversationBreakpoint
            {
                MessageId = cutDialog.MessageId,
                Breakpoint = cutDialog.CreatedAt,
                Reason = summary,
                CreatedTime = DateTime.UtcNow
            };
            await db.UpdateConversationBreakpoint(conversationId, breakpoint);

            // Storage layer: physically archive old raw dialogs and replace them with a summary marker.
            var archivedCount = 0;
            if (setting.CompactStorage)
            {
                var summaryDialog = new DialogElement
                {
                    // Timestamp just before the cut so the breakpoint filter and message-type filter
                    // both keep this marker out of the LLM history (the breakpoint reason carries it).
                    MetaData = new DialogMetaData
                    {
                        Role = AgentRole.System,
                        AgentId = conv.AgentId,
                        MessageId = Guid.NewGuid().ToString(),
                        MessageType = MessageTypeName.Summary,
                        MessageLabel = "compression_summary",
                        ExcludeFromContext = true,
                        CreatedTime = cutDialog.CreatedAt.AddTicks(-1)
                    },
                    Content = summary
                };

                archivedCount = await db.CompactConversationDialogs(conversationId, cutDialog.MessageId, summaryDialog, archiveRawDialogs: true);
            }

            _logger.LogInformation($"Auto-compressed conversation {conversationId}: summarized {toSummarize.Count} messages, archived {archivedCount}.");

            await HookEmitter.Emit<IConversationHook>(_services,
                async hook => await hook.OnConversationCompressed(conversationId, breakpoint, archivedCount),
                conv.AgentId);

            return true;
        }
        catch (Exception ex)
        {
            // Compression must never break the conversation turn.
            _logger.LogError(ex, $"Failed to auto-compress conversation {conversationId}.");
            return false;
        }
        finally
        {
            _compressingConversations.TryRemove(conversationId, out _);
        }
    }

    /// <summary>
    /// Index of the first message to KEEP: aims to keep the last <paramref name="keepRecentCount"/>
    /// messages verbatim, then snaps the cut back to the start of a user turn so a tool-call pair
    /// (assistant tool call + its function result) is never split across the boundary.
    /// Returns a value <= 0 when there is nothing worth compressing.
    /// </summary>
    internal static int ComputeCompressionCut(List<RoleDialogModel> dialogs, int keepRecentCount)
    {
        if (dialogs == null || dialogs.Count == 0)
        {
            return -1;
        }

        var cutIndex = dialogs.Count - Math.Max(0, keepRecentCount);
        while (cutIndex > 0 && dialogs[cutIndex].Role != AgentRole.User)
        {
            cutIndex--;
        }

        return cutIndex;
    }

    private async Task<string> SummarizeForCompression(string summaryAgentId, AutoCompressionSetting setting, List<RoleDialogModel> dialogs)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var summaryAgent = await agentService.LoadAgent(summaryAgentId);

        if (summaryAgent == null)
        {
            _logger.LogWarning($"Summary agent '{summaryAgentId}' has no valid LLM config (provider/model); skipping auto-compression.");
            return string.Empty;
        }

        // The summary agent must own the compression template.
        var foundTemplate = summaryAgent.Templates?.FirstOrDefault(x => x.Name.IsEqualTo(setting.SummaryTemplateName));
        if (foundTemplate == null || foundTemplate.LlmConfig?.IsValid != true)
        {
            _logger.LogWarning($"Summary agent '{summaryAgentId}' does not have template '{setting.SummaryTemplateName}'; skipping auto-compression.");
            return string.Empty;
        }

        var content = GetConversationContent(dialogs, dialogs.Count);
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var prompt = GetPrompt(summaryAgent, setting.SummaryTemplateName, new List<string> { content });

        var provider = foundTemplate.LlmConfig.Provider;
        var model = foundTemplate.LlmConfig.Model;
        var chatCompletion = CompletionProvider.GetChatCompletion(_services, provider, model);
        var response = await chatCompletion.GetChatCompletions(new Agent
        {
            Id = summaryAgent.Id,
            Name = summaryAgent.Name,
            Instruction = prompt,
            LlmConfig = new(foundTemplate.LlmConfig)
        }, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, "Please summarize the conversation above.")
        });

        return response.Content;
    }
}
