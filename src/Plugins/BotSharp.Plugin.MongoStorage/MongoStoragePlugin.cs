using BotSharp.Plugin.MongoStorage.Repository;

namespace BotSharp.Plugin.MongoStorage;

/// <summary>
/// MongoDB as the repository
/// </summary>
public class MongoStoragePlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var dbSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", dbSettings);

        if (dbSettings.Default == "MongoRepository")
        {
            services.AddScoped((IServiceProvider x) =>
            {
                var dbSettings = x.GetRequiredService<BotSharpDatabaseSettings>();
                return new MongoDbContext(dbSettings);
            });

            services.AddScoped<IBotSharpRepository, MongoRepository>();
        }
    }
}
