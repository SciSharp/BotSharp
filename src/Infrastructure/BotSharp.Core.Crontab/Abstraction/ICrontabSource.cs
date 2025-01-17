namespace BotSharp.Core.Crontab.Abstraction;

/// <summary>
/// Provide a cron source for the crontab service.
/// </summary>
public interface ICrontabSource
{
    CrontabItem GetCrontabItem();
}
