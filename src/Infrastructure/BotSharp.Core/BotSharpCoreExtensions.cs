using BotSharp.Abstraction.Functions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using BotSharp.Core.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Messaging;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

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

        ValidateJsonConverters(options);
        services.AddSingleton(options);
    }

    private static void ValidateJsonConverters(BotSharpOptions options)
    {
        var jsonConverters = options.JsonSerializerOptions.Converters;
        if (jsonConverters != null)
        {
            // Remove the default rich message/template message converters if there are user-defined converters
            if (jsonConverters.Count(x => x.Type?.Name == nameof(IRichMessage)) > 1)
            {
                var defaultRichMessageConverter = jsonConverters.First(x => x.Type?.Name == nameof(IRichMessage));
                jsonConverters.Remove(defaultRichMessageConverter);
            }

            if (jsonConverters.Count(x => x.Type?.Name == nameof(ITemplateMessage)) > 1)
            {
                var defaultTemplateMessageConverter = jsonConverters.First(x => x.Type?.Name == nameof(ITemplateMessage));
                jsonConverters.Remove(defaultTemplateMessageConverter);
            }
        }
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
