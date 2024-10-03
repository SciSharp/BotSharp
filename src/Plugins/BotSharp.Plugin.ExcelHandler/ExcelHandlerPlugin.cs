using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.ExcelHandler.Helpers.MySql;
using BotSharp.Plugin.ExcelHandler.Helpers.Sqlite;
using BotSharp.Plugin.ExcelHandler.Hooks;
using BotSharp.Plugin.ExcelHandler.Services;
using BotSharp.Plugin.ExcelHandler.Settings;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.ExcelHandler;

public class ExcelHandlerPlugin : IBotSharpPlugin
{
    public string Id => "c56a8e29-b16f-4d75-8766-8309342130cb";
    public string Name => "Excel Handler";
    public string Description => "Load data from excel file and transform it into a list of JSON format.";
    public string IconUrl => "https://w7.pngwing.com/pngs/162/301/png-transparent-microsoft-excel-logo-thumbnail.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<ExcelHandlerSettings>("ExcelHandler");
        });

        services.AddScoped<IAgentUtilityHook, ExcelHandlerUtilityHook>();
        services.AddScoped<IAgentHook, ExcelHandlerHook>();
        services.AddScoped<ISqliteDbHelpers, SqliteDbHelpers>();
        services.AddScoped<IMySqlDbHelper, MySqlDbHelpers>();
        services.AddScoped<ISqliteService, SqliteService>();
        services.AddScoped<IMySqlService, MySqlService>();
    }
}
