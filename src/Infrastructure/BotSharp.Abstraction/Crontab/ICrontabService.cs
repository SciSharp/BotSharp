namespace BotSharp.Abstraction.Crontab;

public interface ICrontabService
{
    Task<List<CrontabItem>> GetCrontable();
    Task ScheduledTimeArrived(CrontabItem item);
}
