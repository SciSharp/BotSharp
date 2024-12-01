namespace BotSharp.Abstraction.Repositories;

public class BotSharpDatabaseSettings : DatabaseBasicSettings
{
    public string[] Assemblies { get; set; } = [];
    public string FileRepository { get; set; } = string.Empty;
    public string BotSharpMongoDb { get; set; } = string.Empty;
    public string TablePrefix { get; set; } = string.Empty;
    public DbConnectionSetting BotSharp { get; set; } = new();
    public string Redis { get; set; } = string.Empty;
    public bool EnableReplica { get; set; } = true;
}

public class DatabaseBasicSettings
{
    public string Default { get; set; } = string.Empty;
    public DbConnectionSetting DefaultConnection { get; set; } = new();
    public bool EnableSqlLog { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
    public bool EnableRetryOnFailure { get; set; }
}

public class DbConnectionSetting
{
    public string Master { get; set; }
    public string[] Slavers { get; set; }
    public int ConnectionTimeout { get; set; } = 30;
    public int ExecutionTimeout { get; set; } = 30;

    public DbConnectionSetting()
    {
        Slavers = [];
    }
}