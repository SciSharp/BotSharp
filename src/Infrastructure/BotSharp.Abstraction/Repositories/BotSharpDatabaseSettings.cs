namespace BotSharp.Abstraction.Repositories;

public class BotSharpDatabaseSettings : DatabaseBasicSettings
{
    public string[] Assemblies { get; set; }
    public string FileRepository { get; set; }
    public BotSharpMongoDbSetting BotSharpMongoDb { get; set; }
    public string TablePrefix { get; set; }
    public DbConnectionSetting BotSharp { get; set; }
    public string Redis { get; set; }
}

public class DatabaseBasicSettings
{
    public string Default { get; set; }
    public DbConnectionSetting DefaultConnection { get; set; }
    public bool EnableSqlLog { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
    public bool EnableRetryOnFailure { get; set; }
}

public class DbConnectionSetting
{
    public string Master { get; set; }
    public string[] Slavers { get; set; }

    public DbConnectionSetting()
    {
        Slavers = new string[0];
    }
}

public class BotSharpMongoDbSetting
{
    public string ConnectionString { get; set; }
    public string? DbNameIndex { get; set; }
}