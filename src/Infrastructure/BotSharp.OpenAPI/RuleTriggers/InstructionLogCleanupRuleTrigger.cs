using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Abstraction.Rules;

namespace BotSharp.OpenAPI.RuleTriggers
{
    public class InstructionLogCleanupRuleTrigger : IRuleTrigger, ICrontabSource
    {
        public string Channel => ConversationChannel.Crontab;
        public string Name => nameof(InstructionLogCleanupRuleTrigger);
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;

        public CrontabItem GetCrontabItem()
        {
            return new CrontabItem
            {
                Title = nameof(InstructionLogCleanupRuleTrigger),
                Description = "Clean up old instruction logs daily",
                Cron = "0 6 * * *", // Run at 6:00 AM UTC (Midnight Chicago Standard Time)
                TriggerType = CronTabItemTriggerType.MessageQueue
            };
        }
    }
}
