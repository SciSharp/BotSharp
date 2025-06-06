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
using BotSharp.Core.Roles.Services;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Templating;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Repositories.Settings;

namespace BotSharp.Core;

public static class BotSharpCoreExtensions
{
    public static IServiceCollection AddBotSharpCore(this IServiceCollection services, IConfiguration config, Action<BotSharpOptions>? configOptions = null)
    {
        var interpreterSettings = new InterpreterSettings();
        config.Bind("Interpreter", interpreterSettings);
        services.AddSingleton(x => interpreterSettings);

        services.AddSingleton<IDistributedLocker, DistributedLocker>();
        // Register template render
        services.AddSingleton<ITemplateRender, TemplateRender>();

        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ProcessorFactory>();

        AddRedisEvents(services, config);
        // Register cache service
        AddCacheServices(services, config);

        RegisterPlugins(services, config);
        AddBotSharpOptions(services, configOptions);

        return services;
    }

    private static void AddCacheServices(IServiceCollection services, IConfiguration config)
    {
        services.AddMemoryCache();
        var cacheSettings = new SharpCacheSettings();
        config.Bind("SharpCache", cacheSettings);
        services.AddSingleton(x => cacheSettings);

        services.AddSingleton<ICacheService>(sp => cacheSettings.CacheType switch
        {
            CacheType.RedisCache => ActivatorUtilities.CreateInstance<RedisCacheService>(sp),
            _ => ActivatorUtilities.CreateInstance<MemoryCacheService>(sp),
        });
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

        if (string.IsNullOrEmpty(dbSettings.Redis))
        {
            return;
        }

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
        var excludedFunctions = pluginSettings.ExcludedFunctions ?? [];

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
            // Register function callback
            var functions = assembly.GetTypes()
                .Where(x => x.IsClass
                        && x.GetInterface(nameof(IFunctionCallback)) != null
                        && !excludedFunctions.Contains(x.Name))
                .ToArray();

            foreach (var function in functions)
            {
                services.AddScoped(typeof(IFunctionCallback), function);
            }
        });

        services.AddSingleton(loader);
    }
}
