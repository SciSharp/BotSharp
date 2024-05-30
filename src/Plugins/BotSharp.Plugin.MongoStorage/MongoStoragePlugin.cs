using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Plugin.MongoStorage.Repository;

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
            services.AddScoped((IServiceProvider x) =>
            {
                var dbSettings = x.GetRequiredService<BotSharpDatabaseSettings>();
                return new MongoDbContext(dbSettings);
            });

            services.AddScoped<IBotSharpRepository, MongoRepository>();
        }
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("MongoDB", icon: "bx bx-data", link: "page/mongodb", weight: section.Weight + 10)
        {
            Roles = new List<string> { UserRole.Admin }
        });
        return true;
    }
}
