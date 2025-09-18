namespace BotSharp.Plugin.ExcelHandler.Settings;

public class ExcelHandlerSettings
{
    public DatabaseSettings Database { get; set; }
}

public class DatabaseSettings
{
    public string Provider { get; set; } = "mysql";
    public string ConnectionString { get; set; }
}