using BotSharp.Core;
using BotSharp.Core.Infrastructures;
using BotSharp.Core.Plugins;
using BotSharp.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace BotSharp.OpenAPI;

public class ServiceBuilder
{
    public static IServiceProvider CreateHostBuilder(Assembly? startUp = null)
    {
        Console.WriteLine("Creating host builder...");

        // Set up configuration
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        if (startUp != null)
        {
            configurationBuilder.AddUserSecrets(startUp);
        }

        var configuration = configurationBuilder.Build();

        // Create host builder
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddBotSharpCore(context.Configuration, options =>
                {
                })
                .AddBotSharpOpenAPI(context.Configuration, [], context.HostingEnvironment, true)
                .AddBotSharpLogger(context.Configuration);
            });

        // Build the host
        var host = builder.Build();
        var serviceProvider = host.Services;

        // Configure plugins
        serviceProvider.GetRequiredService<PluginLoader>().Configure(null);

        // Set root services for SharpCacheAttribute
        SharpCacheAttribute.Services = serviceProvider;

        return serviceProvider;
    }
}
