using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.TextGeneratives;
using BotSharp.Core.Conversations;
using BotSharp.Core.Plugins.TextGeneratives.LLamaSharp;
using BotSharp.Core.Repository;
using BotSharp.Core.Services;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core;

public static class BotSharpServiceCollectionExtensions
{
    public static IServiceCollection AddBotSharp(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IPlatformMidware, PlatformMidware>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IConversationService, ConversationService>();

        RegisterRepository(services, config);
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

    public static void RegisterRepository(IServiceCollection services, IConfiguration config)
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

        services.AddSingleton(x =>
        {
            var settings = new LlamaSharpSettings();
            config.Bind("LlamaSharp", settings);
            return settings;
        });
        services.AddSingleton<IChatCompletionProvider, ChatCompletionProvider>();
    }
}
