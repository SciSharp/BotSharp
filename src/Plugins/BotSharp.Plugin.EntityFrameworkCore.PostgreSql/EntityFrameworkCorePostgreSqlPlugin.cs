using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Plugin.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Collections.Generic;
using System.Linq;

namespace BotSharp.Plugin.EntityFrameworkCore.PostgreSql;

public class EntityFrameworkCorePostgreSqlPlugin : IBotSharpPlugin
{
    public string Id => "a1df76ef-20dc-403e-b375-7f8f5d42250d";
    public string Name => "PostgreSql Storage";
    public string Description => "PostgreSql as the repository, PostgreSQL: The World's Most Advanced Open Source Relational Database.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/128/10464/10464288.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var dbSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", dbSettings);

        if (dbSettings.Default == RepositoryEnum.PostgreSqlRepository)
        {
            services.AddSingleton(dbSettings);

            var dataSource = new NpgsqlDataSourceBuilder(dbSettings.BotSharpPostgreSql).EnableDynamicJson().Build();

            services.AddDbContext<BotSharpEfCoreDbContext>(options => options.UseNpgsql(dataSource, x => x.MigrationsAssembly("BotSharp.Plugin.EntityFrameworkCore.PostgreSql")));

            services.AddScoped<IBotSharpRepository, EfCoreRepository>();
        }
    }


    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("PostgreSql", icon: "bx bx-data", link: "page/pgsql", weight: section.Weight + 10)
        {
            Roles = new List<string> { UserRole.Admin }
        });
        return true;
    }
}
