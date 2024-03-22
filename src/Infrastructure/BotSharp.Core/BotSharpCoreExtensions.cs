using BotSharp.Abstraction.Functions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using BotSharp.Core.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Messaging.JsonConverters;

namespace BotSharp.Core;

public static class BotSharpCoreExtensions
{
    public static IServiceCollection AddBotSharpCore(this IServiceCollection services, IConfiguration config, Action<BotSharpOptions>? configOptions = null)
    {
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IUserService, UserService>();

        RegisterPlugins(services, config);
        ConfigureBotSharpOptions(services, configOptions);
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

    private static void ConfigureBotSharpOptions(IServiceCollection services, Action<BotSharpOptions>? configure)
    {
        var options = new BotSharpOptions();
        if (configure != null)
        {
            configure(options);
        }

        AddDefaultJsonConverters(options);
        services.AddSingleton(options);
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
