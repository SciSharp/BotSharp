using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
using BotSharp.OpenAPI.RuleTriggers;
using Microsoft.Extensions.Logging;

namespace BotSharp.OpenAPI.Hooks
{
    public class IdleConversationCleanupCrontabHook : ICrontabHook
    {
        private readonly ConversationSetting _settings;
        private readonly IConversationService _conversationService;
        private readonly ILogger<IdleConversationCleanupCrontabHook> _logger;

        public string[]? Triggers => new[] { nameof(IdleConversationCleanupRuleTrigger) };

        public IdleConversationCleanupCrontabHook(
            ConversationSetting settings,
            IConversationService conversationService,
            ILogger<IdleConversationCleanupCrontabHook> logger)
        {
            _settings = settings;
            _conversationService = conversationService;
            _logger = logger;
        }

        public async Task OnCronTriggered(CrontabItem item)
        {
            var cleanSetting = _settings.CleanSetting;

            if (cleanSetting == null || !cleanSetting.Enable) return;

            try
            {
                var batchSize = cleanSetting.BatchSize;
                var limit = cleanSetting.MessageLimit;
                var bufferHours = cleanSetting.BufferHours;
                var excludeAgentIds = cleanSetting.ExcludeAgentIds ?? new List<string>();
                var conversationIds = await _conversationService.GetIdleConversations(batchSize, limit, bufferHours, excludeAgentIds);

                if (!conversationIds.IsNullOrEmpty())
                {
                    await _conversationService.DeleteConversations(conversationIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred closing conversations.");
            }
        }
    }
}
