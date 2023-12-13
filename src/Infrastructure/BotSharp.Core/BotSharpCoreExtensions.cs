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
using BotSharp.Core.Routing.Hooks;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Core.Plugins;
using BotSharp.Abstraction.Evaluations.Settings;
using BotSharp.Abstraction.Evaluations;
using BotSharp.Core.Evaluatings;
using BotSharp.Core.Evaluations;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Planning;
using BotSharp.Core.Planning;
using BotSharp.Abstraction.MLTasks;
using static Dapper.SqlMapper;

namespace BotSharp.Core;

public static class BotSharpCoreExtensions
{
    public static IServiceCollection AddBotSharpCore(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILlmProviderSettingService, LlmProviderSettingService>();
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
        services.AddScoped<RoutingContext>();

        var databaseSettings = new DatabaseBasicSettings();
        config.Bind("Database", databaseSettings);
        services.AddSingleton((IServiceProvider x) => databaseSettings);

        var myDatabaseSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", myDatabaseSettings);
        services.AddSingleton((IServiceProvider x) => myDatabaseSettings);

        var llmProviders = new List<LlmProviderSetting>();
        config.Bind("LlmProviders", llmProviders);
        services.AddSingleton((IServiceProvider x) =>
        {
            foreach (var llmProvider in llmProviders)
            {
                Console.WriteLine($"Loaded LlmProvider {llmProvider.Provider} settings with {llmProvider.Models.Count} models.");
            }
            return llmProviders;
        });

        RegisterPlugins(services, config);

        // Register template render
        services.AddSingleton<ITemplateRender, TemplateRender>();
        services.AddScoped<IResponseTemplateService, ResponseTemplateService>();

        // Register router
        var routingSettings = new RoutingSettings();
        config.Bind("Router", routingSettings);
        services.AddSingleton((IServiceProvider x) => routingSettings);

        services.AddScoped<NaivePlanner>();
        services.AddScoped<HFPlanner>();
        services.AddScoped<IPlaner>(provider =>
        {
            if (routingSettings.Planner == nameof(HFPlanner))
                return provider.GetRequiredService<HFPlanner>();
            else
                return provider.GetRequiredService<NaivePlanner>();
        });

        services.AddScoped<IExecutor, InstructExecutor>();
        services.AddScoped<IRoutingService, RoutingService>();

        if (myDatabaseSettings.Default == "FileRepository")
        {
            services.AddScoped<IBotSharpRepository, FileRepository>();
        }

        services.AddScoped<IInstructService, InstructService>();
        services.AddScoped<ITokenStatistics, TokenStatistics>();

        services.AddScoped<IAgentHook, RoutingAgentHook>();

        // Evaluation
        var evalSetting = new EvaluatorSetting();
        config.Bind("Evaluator", evalSetting);
        services.AddSingleton((IServiceProvider x) => evalSetting);

        services.AddScoped<IConversationHook, EvaluationConversationHook>();
        services.AddScoped<IEvaluatingService, EvaluatingService>();
        services.AddScoped<IExecutionLogger, ExecutionLogger>();

        services.AddScoped<IConversationAttachmentService, ConversationAttachmentService>();

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
        var pluginSettings = new PluginSettings();
        config.Bind("PluginLoader", pluginSettings);
        services.AddSingleton(pluginSettings);

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
