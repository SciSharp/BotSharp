using BotSharp.Plugin.Mongo.Repository;

namespace BotSharp.Plugin.Mongo;

public class MongoRepositoryPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton((IServiceProvider x) =>
        {
            var databaseSettings = x.GetRequiredService<MyDatabaseSettings>();
            return new MongoDbContext(databaseSettings.MongoDb);
        });

        services.AddScoped<IBotSharpRepository, MongoRepository>();
    }
}
