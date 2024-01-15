using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Settings;
using BotSharp.Core.Planning;
using BotSharp.Core.Routing.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Routing;

public class RoutingPlugin : IBotSharpPlugin
{
    public string Id => "87352ece-2c1c-477c-8236-2e047e11dcab";

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

        services.AddScoped<NaivePlanner>();
        services.AddScoped<HFPlanner>();
        services.AddScoped<IPlaner>(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            var routingSettings = settingService.Bind<RoutingSettings>("Router");
            if (routingSettings.Planner == nameof(HFPlanner))
                return provider.GetRequiredService<HFPlanner>();
            else
                return provider.GetRequiredService<NaivePlanner>();
        });
    }
}
