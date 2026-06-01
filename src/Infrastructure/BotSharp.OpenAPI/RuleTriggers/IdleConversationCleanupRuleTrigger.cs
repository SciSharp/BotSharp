using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Abstraction.Rules;

namespace BotSharp.OpenAPI.RuleTriggers
{
    public class IdleConversationCleanupRuleTrigger : IRuleTrigger, ICrontabSource
    {
        public string Channel => ConversationChannel.Crontab;
        public string Name => nameof(IdleConversationCleanupRuleTrigger);
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;

        public CrontabItem GetCrontabItem()
        {
            return new CrontabItem
            {
                Title = nameof(IdleConversationCleanupRuleTrigger),
                Description = "Clean up idle conversations hourly",
                Cron = "0 * * * *",
                TriggerType = CronTabItemTriggerType.MessageQueue
            };
        }
    }
}
