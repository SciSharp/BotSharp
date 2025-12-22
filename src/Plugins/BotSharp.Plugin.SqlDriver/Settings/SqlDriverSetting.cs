namespace BotSharp.Plugin.SqlDriver.Settings;

public class SqlDriverSetting
{
    public DataSourceSetting[] Connections { get; set; } = [];
    public bool ExecuteSqlSelectAutonomous { get; set; } = false;
    public bool FormattingResult { get; set; } = true;
}
