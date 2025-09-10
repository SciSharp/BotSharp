namespace BotSharp.Abstraction.Crontab.Settings;

public class CrontabSettings
{
    public CrontabBaseSetting EventSubscriber { get; set; } = new();
    public CrontabBaseSetting Watcher { get; set; } = new();
    public DebugSetting Debug { get; set; } = new();
}

public class CrontabBaseSetting
{
    public bool Enabled { get; set; } = true;
}

public class DebugSetting
{
    public string AllowRuleTrigger { get; set; } = "";
}
