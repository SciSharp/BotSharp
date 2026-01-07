using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Core.A2A.Functions;
using BotSharp.Core.A2A.Hooks;
using BotSharp.Core.A2A.Services;
using BotSharp.Core.A2A.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core.A2A;

public class A2APlugin : IBotSharpPlugin
{
 
    public string Id => "058cdf87-fcf3-eda9-915a-565c04bc9f56";

    public string Name => "A2A Protocol Integration";

    public string Description => "Enables seamless integration with external agents via the Agent-to-Agent (A2A) protocol.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    { 
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<A2ASettings>("A2AIntegration");
        });

        services.AddScoped<IA2AService, A2AService>();
        services.AddScoped<IAgentHook, A2AAgentHook>(); 
        services.AddScoped<IFunctionCallback, A2ADelegationFn>();
    }
}
