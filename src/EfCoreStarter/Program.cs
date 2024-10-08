using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Plugin.EntityFrameworkCore;
using BotSharp.Plugin.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var dbSettings = new BotSharpDatabaseSettings();
builder.Configuration.Bind("Database", dbSettings);

builder.Services.AddSingleton(dbSettings);

if (dbSettings.Default == RepositoryEnum.PostgreSqlRepository)
{
    builder.Services.AddDbContext<BotSharpEfCoreDbContext>(options =>
    {
        options.UseNpgsql(dbSettings.BotSharpPostgreSql, x => x.MigrationsAssembly("BotSharp.Plugin.EntityFrameworkCore.PostgreSql"));
    });

}

if (dbSettings.Default == RepositoryEnum.MySqlRepository)
{
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 39));
    builder.Services.AddMySql<BotSharpEfCoreDbContext>(dbSettings.BotSharpMySql, serverVersion, s =>
                  s.UseMicrosoftJson().MigrationsAssembly("BotSharp.Plugin.EntityFrameworkCore.MySql"));
}

builder.Services.AddScoped<IBotSharpRepository, EfCoreRepository>();

var app = builder.Build();

app.Run();