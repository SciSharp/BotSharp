using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Repository;

public class RepositoryPlugin : IBotSharpPlugin
{
    public string Id => "866b4b19-b4d3-479d-8e0a-98816643b8db";
    public string Name => "Data Repository";
    public string Description => "Provides a data persistence abstraction layer to store Agent and conversation-related data.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var myDatabaseSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", myDatabaseSettings);

        // In order to use EntityFramework.BootKit in other plugin
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<DatabaseSettings>("Database");
        });

        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<DatabaseBasicSettings>("Database");
        });

        services.AddSingleton(provider => myDatabaseSettings);

        if (myDatabaseSettings.Default == RepositoryEnum.FileRepository)
        {
            services.AddScoped<IBotSharpRepository, FileRepository>();
        }
    }
}
