using BotSharp.Abstraction.Conversations;
using BotSharp.Core.Conversations;
using BotSharp.Core.Repository;
using BotSharp.Core.Services;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core;

public static class BotSharpServiceCollectionExtensions
{
    public static IServiceCollection AddBotSharp(this IServiceCollection services)
    {
        services.AddScoped<IPlatformMidware, PlatformMidware>();
        services.AddSingleton<IConversationService, ConversationService>();

        return services;
    }

    public static void ConfigureBotSharp(this IServiceCollection services)
    {
    }

    public static IApplicationBuilder UseBotSharp(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app;
    }

    public static void RegisterRepository(this IServiceCollection services, IConfiguration config)
    {
        var databaseSettings = new DatabaseSettings();
        config.Bind("Database", databaseSettings);
        services.AddSingleton((IServiceProvider x) =>
        {
            return databaseSettings;
        });

        var myDatabaseSettings = new MyDatabaseSettings();
        config.Bind("Database", myDatabaseSettings);
        services.AddSingleton((IServiceProvider x) =>
        {
            return databaseSettings;
        });

        services.AddScoped((IServiceProvider x) =>
        {
            return DataContextHelper.GetDbContext<MongoDbContext>(myDatabaseSettings, x);
        });
    }
}
