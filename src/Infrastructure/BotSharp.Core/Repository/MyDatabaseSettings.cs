namespace BotSharp.Core.Repository;

public class MyDatabaseSettings : DatabaseSettings
{
    public string[] Assemblies { get; set; }
    public string FileRepository { get; set; }
    public DbConnectionSetting MongoDb { get; set; }
    public DbConnectionSetting BotSharp { get; set; }
}
