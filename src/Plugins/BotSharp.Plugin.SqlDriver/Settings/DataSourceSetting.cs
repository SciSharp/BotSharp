namespace BotSharp.Plugin.SqlDriver.Settings;


public class DataSourceSetting
{
    public string Name { get; set; } = "default";
    public string DbType { get; set; } = "mysql";
    public string ConnectionString { get; set; } = "localhost";
}
