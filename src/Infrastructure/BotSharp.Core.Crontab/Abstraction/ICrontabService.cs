namespace BotSharp.Core.Crontab.Abstraction;

public interface ICrontabService
{
    Task<List<CrontabItem>> GetCrontable();
    Task ScheduledTimeArrived(CrontabItem item);
}
