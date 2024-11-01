namespace BotSharp.Plugin.SqlHero.Settings;

public class SqlDriverSetting
{
    public string DatabaseType { get; set; } = "mysql";
    public string MySqlConnectionString { get; set; } = null!;
    public string MySqlExecutionConnectionString { get; set; } = null!;
    public string MySqlTempConnectionString { get; set; } = null!;
    public string MySqlMetaConnectionString { get; set; } = null!;
    public string SqlServerConnectionString { get; set; } = null!;
    public string SqlServerExecutionConnectionString { get; set; } = null!;
    public string SqlLiteConnectionString { get; set; } = null!;
    public string RedshiftConnectionString { get; set; } = null!;
    public bool ExecuteSqlSelectAutonomous { get; set; } = false;
    public bool FormattingResult { get; set; } = true;
}
