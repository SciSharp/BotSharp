using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Infrastructures.ContentTransmitters;
using BotSharp.Abstraction.Users;
using BotSharp.Core.Agents.Services;
using BotSharp.Core.Conversations.Services;
using BotSharp.Core.Infrastructures;
using BotSharp.Core.Users.Services;
using BotSharp.Plugins.LLamaSharp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core;

public static class BotSharpServiceCollectionExtensions
{
    public static IServiceCollection AddBotSharp(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IConversationService, ConversationService>();

        services.AddScoped<IContentTransfer, ContentTransfer>();

        RegisterRepository(services, config);

        RegisterPlugins(services, config);

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

        services.AddScoped((IServiceProvider x) =>
        {
            return DataContextHelper.GetDbContext<AgentDbContext>(myDatabaseSettings, x);
        });
    }

    public static void RegisterPlugins(IServiceCollection services, IConfiguration config)
    {
        var settings = new LlamaSharpSettings();
        config.Bind("LlamaSharp", settings);
        services.AddSingleton(x =>
        {

            return settings;
        });

        // services.AddScoped<IServiceZone, ChatCompletionProvider>();
    }
}
