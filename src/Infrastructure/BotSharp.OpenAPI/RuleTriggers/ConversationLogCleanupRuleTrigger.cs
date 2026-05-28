using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Abstraction.Rules;

namespace BotSharp.OpenAPI.RuleTriggers
{
    public class ConversationLogCleanupRuleTrigger : IRuleTrigger, ICrontabSource
    {
        public string Channel => ConversationChannel.Crontab;
        public string Name => nameof(ConversationLogCleanupRuleTrigger);
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;

        public CrontabItem GetCrontabItem()
        {
            return new CrontabItem
            {
                Title = nameof(ConversationLogCleanupRuleTrigger),
                Description = "Clean up old conversation logs daily",
                Cron = "0 6 * * *", // Run at 6:00 AM UTC (Midnight Chicago Standard Time)
                TriggerType = CronTabItemTriggerType.MessageQueue
            };
        }
    }
}
