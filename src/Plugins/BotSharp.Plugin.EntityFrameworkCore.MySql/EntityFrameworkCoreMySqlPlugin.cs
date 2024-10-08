using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Plugin.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BotSharp.Plugin.EntityFrameworkCore.PostgreSql;

public class EntityFrameworkCoreMySqlPlugin : IBotSharpPlugin
{
    public string Id => "accbf103-7d82-4e97-869f-db471ac99ae8";
    public string Name => "MySql Storage";
    public string Description => "MySql as the repository, MySQL is a popular open source database management system that supports various workloads and cloud providers. ";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/128/15484/15484291.png";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var dbSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", dbSettings);

        if (dbSettings.Default == RepositoryEnum.MySqlRepository)
        {
            services.AddSingleton(dbSettings);

            var serverVersion = new MySqlServerVersion(new Version(8, 0, 39));

            services.AddMySql<BotSharpEfCoreDbContext>(dbSettings.BotSharpMySql, serverVersion, s =>
                          s.UseMicrosoftJson().MigrationsAssembly("BotSharp.Plugin.EntityFrameworkCore.MySql"));

            services.AddScoped<IBotSharpRepository, EfCoreRepository>();
        }
    }


    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("MySql", icon: "bx bx-data", link: "page/mysql", weight: section.Weight + 10)
        {
            Roles = new List<string> { UserRole.Admin }
        });
        return true;
    }
}
