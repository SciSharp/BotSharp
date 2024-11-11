using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using BotSharp.Abstraction.Functions;
using BotSharp.Core.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Messaging.JsonConverters;
using BotSharp.Abstraction.Users.Settings;
using BotSharp.Abstraction.Interpreters.Settings;
using BotSharp.Abstraction.Infrastructures;
using BotSharp.Core.Processors;
using StackExchange.Redis;
using BotSharp.Core.Infrastructures.Events;

namespace BotSharp.Core;

public static class BotSharpCoreExtensions
{
    public static IServiceCollection AddBotSharpCore(this IServiceCollection services, IConfiguration config, Action<BotSharpOptions>? configOptions = null)
    {
        var interpreterSettings = new InterpreterSettings();
        config.Bind("Interpreter", interpreterSettings);
        services.AddSingleton(x => interpreterSettings);

        services.AddSingleton<DistributedLocker>();

        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ProcessorFactory>();

        // Register cache service
        var cacheSettings = new SharpCacheSettings();
        config.Bind("SharpCache", cacheSettings);
        services.AddSingleton(x => cacheSettings);
        services.AddSingleton<ICacheService, RedisCacheService>();

        AddRedisEvents(services, config);

        services.AddMemoryCache();

        RegisterPlugins(services, config);
        AddBotSharpOptions(services, configOptions);

        return services;
    }

    public static IServiceCollection UsingSqlServer(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IBotSharpRepository>(sp =>
        {
            var myDatabaseSettings = sp.GetRequiredService<BotSharpDatabaseSettings>();
            return DataContextHelper.GetSqlServerDbContext<BotSharpDbContext>(myDatabaseSettings, sp);
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

        // Set root services for SharpCacheAttribute
        SharpCacheAttribute.Services = app.ApplicationServices;

        return app;
    }

    private static void AddBotSharpOptions(IServiceCollection services, Action<BotSharpOptions>? configure)
    {
        var options = new BotSharpOptions();
        if (configure != null)
        {
            configure(options);
        }

        AddDefaultJsonConverters(options);
        services.AddSingleton(options);
    }

    private static void AddRedisEvents(IServiceCollection services, IConfiguration config)
    {
        // Add Redis connection as a singleton
        var dbSettings = new BotSharpDatabaseSettings();
        config.Bind("Database", dbSettings);

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(dbSettings.Redis));
        services.AddSingleton<IEventPublisher, RedisPublisher>();
        services.AddSingleton<IEventSubscriber, RedisSubscriber>();
    }

    private static void AddDefaultJsonConverters(BotSharpOptions options)
    {
        options.JsonSerializerOptions.Converters.Add(new RichContentJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new TemplateMessageJsonConverter());
    }

    public static void RegisterPlugins(IServiceCollection services, IConfiguration config)
    {
        var pluginSettings = new PluginSettings();
        config.Bind("PluginLoader", pluginSettings);
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<PluginSettings>("PluginLoader");
        });

        var accountSettings = new AccountSetting();
        config.Bind("Account", accountSettings);
        services.AddScoped(x => accountSettings);

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
