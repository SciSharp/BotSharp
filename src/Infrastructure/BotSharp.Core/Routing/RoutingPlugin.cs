using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Settings;
using BotSharp.Core.Routing.Hooks;
using BotSharp.Core.Routing.Planning;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Routing;

public class RoutingPlugin : IBotSharpPlugin
{
    public string Id => "87352ece-2c1c-477c-8236-2e047e11dcab";
    public string Name => "Agent Routing";
    public string Description => "Based on the conversation context, routing user request to the appropriate Agent. It's designed for complicated tasks.";

    public SettingsMeta Settings =>
        new SettingsMeta("Router");

    public object GetNewSettingsInstance() =>
         new RoutingSettings();

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<RoutingContext>();

        // Register router
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<RoutingSettings>("Router");
        });

        services.AddScoped<IRoutingService, RoutingService>();
        services.AddScoped<IAgentHook, RoutingAgentHook>();

        services.AddScoped<IPlaner, NaivePlanner>();
        services.AddScoped<IPlaner, HFPlanner>();
        services.AddScoped<IPlaner, SequentialPlanner>();
        services.AddScoped<IPlaner, TwoStagePlanner>();
    }
}
