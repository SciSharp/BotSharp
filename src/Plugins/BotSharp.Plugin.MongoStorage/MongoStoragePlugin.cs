using BotSharp.Plugin.MongoStorage.Repository;

namespace BotSharp.Plugin.MongoStorage;

/// <summary>
/// MongoDB as the repository
/// </summary>
public class MongoStoragePlugin : IBotSharpPlugin
{
    public string Id => "058094a7-4ad3-4284-b94d-ac1373cf63d8";
    public string Name => "MongoDB Storage";
    public string Description => "MongoDB as the repository, store data in document.";
    public string IconUrl => "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRrFrT-_0VYV4PraApwSUmsf4pBGWgvLTaLZGUd7942FxjErsA5iaL4n5Q7CplOmVtwEQ&usqp=CAU";

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
