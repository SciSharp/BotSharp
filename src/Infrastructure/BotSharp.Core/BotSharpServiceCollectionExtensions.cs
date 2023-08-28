using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Core.Functions;
using BotSharp.Core.Hooks;
using BotSharp.Core.Routing;
using BotSharp.Core.Templating;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core;

public static class BotSharpServiceCollectionExtensions
{
    public static IServiceCollection AddBotSharp(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IAgentService, AgentService>();

        var agentSettings = new AgentSettings();
        config.Bind("Agent", agentSettings);
        services.AddSingleton((IServiceProvider x) => agentSettings);

        var convsationSettings = new ConversationSetting();
        config.Bind("Conversation", convsationSettings);
        services.AddSingleton((IServiceProvider x) => convsationSettings);

        services.AddScoped<IConversationStorage, ConversationStorage>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IConversationStateService, ConversationStateService>();

        var databaseSettings = new DatabaseSettings();
        config.Bind("Database", databaseSettings);
        services.AddSingleton((IServiceProvider x) => databaseSettings);

        var myDatabaseSettings = new MyDatabaseSettings();
        config.Bind("Database", myDatabaseSettings);
        services.AddSingleton((IServiceProvider x) => myDatabaseSettings);

        RegisterPlugins(services, config);

        // Register template render
        services.AddSingleton<TemplateRender>();

        // Register router
        services.AddScoped<Router>();
        services.AddScoped<Reasoner>();
        services.AddScoped<IAgentRouting>(p =>
        {
            var setting = p.GetRequiredService<ConversationSetting>();
            return setting.EnableReasoning ? p.GetRequiredService<Reasoner>() : p.GetRequiredService<Router>();
        });

        // Register function callback
        services.AddScoped<IFunctionCallback, RouteToAgentFn>();

        // Register Hooks
        services.AddScoped<IAgentHook, RoutingHook>();

        services.AddScoped<Simulator>();

        return services;
    }

    public static IServiceCollection UsingSqlServer(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IBotSharpRepository>(sp =>
        {
            var myDatabaseSettings = sp.GetRequiredService<MyDatabaseSettings>();
            return DataContextHelper.GetDbContext<BotSharpDbContext, DbContext4SqlServer>(myDatabaseSettings, sp);
        });

        return services;
    }

    public static IServiceCollection UsingFileRepository(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IBotSharpRepository>(sp =>
        {
            var myDatabaseSettings = sp.GetRequiredService<MyDatabaseSettings>();
            return new FileRepository(myDatabaseSettings, sp);
        });

        return services;
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

    public static void RegisterPlugins(IServiceCollection services, IConfiguration config)
    {
        var pluginSettings = new PluginLoaderSettings();
        config.Bind("PluginLoader", pluginSettings);

        var loader = new PluginLoader(services, config, pluginSettings);
        loader.Load();

        services.AddSingleton(loader);
    }
}
