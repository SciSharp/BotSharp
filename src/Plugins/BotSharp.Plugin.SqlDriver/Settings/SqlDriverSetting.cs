namespace BotSharp.Plugin.SqlHero.Settings;

public class SqlDriverSetting
{
    public string DatabaseType { get; set; } = "MySql";
    public string MySqlConnectionString { get; set; } = null!;
    public string MySqlExecutionConnectionString { get; set; } = null!;
    public string SqlServerConnectionString { get; set; } = null!;
    public string SqlServerExecutionConnectionString { get; set; } = null!;
    public string SqlLiteConnectionString { get; set; } = null!;
    public bool ExecuteSqlSelectAutonomous { get; set; } = false;
}
