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
            var currentMinute = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                   currentTime.Hour, currentTime.Minute, 0, DateTimeKind.Utc);

            var oneMinuteAgo = currentMinute.AddMinutes(-1);
            var nextOccurrence = schedule.GetNextOccurrence(oneMinuteAgo);

            return nextOccurrence == currentMinute;
        }
    }
}
