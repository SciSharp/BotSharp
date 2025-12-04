using NCrontab;

namespace BotSharp.Core.Crontab.Services
{
    public static class CrontabItemExtension
    {
        public static bool CheckNextOccurrenceEveryOneMinute(this CrontabItem item)
        {
            // strip seconds from cron expression
            item.Cron = string.Join(" ", item.Cron.Split(' ').TakeLast(5));
            var schedule = CrontabSchedule.Parse(item.Cron, new CrontabSchedule.ParseOptions
            {
                IncludingSeconds = false // Ensure you account for seconds
            });

            var currentTime = DateTime.UtcNow;

            // Check if there has been an execution point within the past minute.
            var oneMinuteAgo = currentTime.AddMinutes(-1);
            var nextOccurrenceFromPast = schedule.GetNextOccurrence(oneMinuteAgo);

            // If the next execution point falls within the past minute up to the present, then it matches.
            return nextOccurrenceFromPast > oneMinuteAgo && nextOccurrenceFromPast <= currentTime;
        }
    }
}
