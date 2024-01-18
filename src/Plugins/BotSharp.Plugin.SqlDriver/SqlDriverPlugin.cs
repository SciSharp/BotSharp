using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.SqlHero.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.SqlDriver;

public class SqlDriverPlugin : IBotSharpPlugin
{
    public string Id => "da7b6f7a-b1f0-455a-9939-ad2d493e929e";
    public string Name => "SQL Driver";
    public string Description => "Convert the requirements into corresponding SQL statements and execute if needed";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new SqlDriverSetting();
        config.Bind("SqlDriver", settings);
        services.AddSingleton(x =>
        {
            Console.WriteLine($"Loaded SqlHero settings:: {Regex.Replace(settings.MySqlConnectionString, "password=.*?;", "password=******;")}", Color.Yellow);
            return settings;
        });
    }
}
