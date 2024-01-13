using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.SqlHero.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.SqlHero;

public class SqlHeroPlugin : IBotSharpPlugin
{
    public string Name => "SQL Hero";
    public string Description => "Convert the requirements into corresponding SQL statements and execute if needed";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new SqlHeroSetting();
        config.Bind("SqlHero", settings);
        services.AddSingleton(x =>
        {
            Console.WriteLine($"Loaded SqlHero settings:: {Regex.Replace(settings.MySqlConnectionString, "password=.*?;", "password=******;")}", Color.Yellow);
            return settings;
        });
    }
}
