using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Repositories;
using BotSharp.Core.Routing;
using BotSharp.Core.Templating;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Instructs;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Routing;
using System.Reflection;

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

        var databaseSettings = new DatabaseBasicSettings();
        config.Bind("Database", databaseSettings);
        services.AddSingleton((IServiceProvider x) => databaseSettings);

        var myDatabaseSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", myDatabaseSettings);
        services.AddSingleton((IServiceProvider x) => myDatabaseSettings);

        RegisterPlugins(services, config);

        // Register template render
        services.AddSingleton<ITemplateRender, TemplateRender>();
        services.AddScoped<IResponseTemplateService, ResponseTemplateService>();

        // Register router
        var routingSettings = new RoutingSettings();
        config.Bind("Router", routingSettings);
        services.AddSingleton((IServiceProvider x) => routingSettings);

        services.AddScoped<IAgentRouting, Router>();
        services.AddScoped<IRoutingService, RoutingService>();

        if (myDatabaseSettings.Default == "FileRepository")
        {
            services.AddScoped<IBotSharpRepository, FileRepository>();
        }

        services.AddScoped<IInstructService, InstructService>();

        return services;
    }

    public static IServiceCollection UsingSqlServer(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IBotSharpRepository>(sp =>
        {
            var myDatabaseSettings = sp.GetRequiredService<BotSharpDatabaseSettings>();
            return DataContextHelper.GetDbContext<BotSharpDbContext, DbContext4SqlServer>(myDatabaseSettings, sp);
        });

        return services;
    }

    //public static IServiceCollection UsingFileRepository(this IServiceCollection services, IConfiguration config)
    //{
    //    services.AddScoped<IBotSharpRepository>(sp =>
    //    {
    //        var myDatabaseSettings = sp.GetRequiredService<BotSharpDatabaseSettings>();
    //        return new FileRepository(myDatabaseSettings, sp);
    //    });

    //    return services;
    //}

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
        loader.Load(assembly =>
        {
            // Register routing handlers
            var handlers = assembly.GetTypes()
                .Where(x => x.IsClass)
                .Where(x => x.GetInterface(nameof(IRoutingHandler)) != null)
                .ToArray();

            foreach (var handler in handlers)
            {
                services.AddScoped(typeof(IRoutingHandler), handler);
            }

            // Register function callback
            var functions = assembly.GetTypes()
                .Where(x => x.IsClass)
                .Where(x => x.GetInterface(nameof(IFunctionCallback)) != null)
                .ToArray();

            foreach (var function in functions)
            {
                services.AddScoped(typeof(IFunctionCallback), function);
            }
        });

        services.AddSingleton(loader);
    }
}
