using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Repositories.Settings;
using BotSharp.Plugin.MongoStorage.Repository;
using MongoDB.Bson.Serialization.Conventions;

namespace BotSharp.Plugin.MongoStorage;

/// <summary>
/// MongoDB as the repository
/// </summary>
public class MongoStoragePlugin : IBotSharpPlugin
{
    public string Id => "058094a7-4ad3-4284-b94d-ac1373cf63d8";
    public string Name => "MongoDB Storage";
    public string Description => "MongoDB as the repository, store data in document DB. It is suitable for production-level systems.";
    public string IconUrl => "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRrFrT-_0VYV4PraApwSUmsf4pBGWgvLTaLZGUd7942FxjErsA5iaL4n5Q7CplOmVtwEQ&usqp=CAU";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var dbSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", dbSettings);

        if (dbSettings.Default == RepositoryEnum.MongoRepository)
        {
            var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            services.AddScoped((IServiceProvider x) =>
            { 
                var tenantEnabled = x.GetService<ITenantFeature>()?.Enabled ?? false;
                if (tenantEnabled)
                {
                    var provider = x.GetService<ITenantConnectionProvider>();
                    if (provider != null)
                    {
                        var cs = provider.GetConnectionString("BotSharpMongoDb");
                        if (!string.IsNullOrWhiteSpace(cs)) dbSettings.BotSharpMongoDb = cs;
                    }
                }

                return new MongoDbContext(dbSettings);
            });

            services.AddScoped<IBotSharpRepository, MongoRepository>();
        }
    }
}
