using BotSharp.Abstraction.Conversations.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core;

public static class BotSharpServiceCollectionExtensions
{
    public static IServiceCollection AddBotSharp(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IUserIdentity, UserIdentity>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IAgentService, AgentService>();

        var convsationSettings = new ConversationSetting();
        config.Bind("Conversation", convsationSettings);
        services.AddSingleton((IServiceProvider x) => convsationSettings);

        services.AddScoped<IConversationStorage, ConversationStorage>();
        services.AddScoped<IConversationService, ConversationService>();

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

        app.ApplicationServices.GetRequiredService<PluginLoader>().Configure(app);

        return app;
    }

    public static void RegisterRepository(IServiceCollection services, IConfiguration config)
    {
        var databaseSettings = new DatabaseSettings();
        config.Bind("Database", databaseSettings);
        services.AddSingleton((IServiceProvider x) => databaseSettings);

        var myDatabaseSettings = new MyDatabaseSettings();
        config.Bind("Database", myDatabaseSettings);
        services.AddSingleton((IServiceProvider x) => databaseSettings);

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
        var pluginSettings = new PluginLoaderSettings();
        config.Bind("PluginLoader", pluginSettings);

        var loader = new PluginLoader(services, config, pluginSettings);
        loader.Load();

        services.AddSingleton(loader);
    }
}
