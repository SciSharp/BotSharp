namespace BotSharp.Core.Crontab.Abstraction;

/// <summary>
/// Provide a cron source for the crontab service.
/// </summary>
public interface ICrontabSource
{
    /// <summary>
    /// Set to true if the cron is real-time like Change Data Capture (CDC).
    /// </summary>
    bool IsRealTime => false;

    CrontabItem GetCrontabItem();
}
