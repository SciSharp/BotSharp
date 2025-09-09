namespace BotSharp.Abstraction.Crontab.Settings;

public class CrontabSettings
{
    public CrontabBaseSetting EventSubscriber { get; set; } = new();
    public CrontabBaseSetting Watcher { get; set; } = new();
    public string LockName { get; set; } = "default";
}

public class CrontabBaseSetting
{
    public bool Enabled { get; set; } = true;
}
