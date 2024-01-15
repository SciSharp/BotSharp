using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Repository;

public class RepositoryPlugin : IBotSharpPlugin
{
    public string Id => "866b4b19-b4d3-479d-8e0a-98816643b8db";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<DatabaseBasicSettings>("Database");
        });

        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<BotSharpDatabaseSettings>("Database");
        });

        var myDatabaseSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", myDatabaseSettings);
        if (myDatabaseSettings.Default == "FileRepository")
        {
            services.AddScoped<IBotSharpRepository, FileRepository>();
        }
    }
}
