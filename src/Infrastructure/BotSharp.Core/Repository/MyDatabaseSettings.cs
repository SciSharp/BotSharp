using EntityFrameworkCore.BootKit;

namespace BotSharp.Core.Repository;

public class MyDatabaseSettings : DatabaseSettings
{
    public string[] Assemblies { get; set; }
    public DbConnectionSetting MongoDb { get; set; }
    public DbConnectionSetting Agent { get; set; }
}
