using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Agents;

public class AgentPlugin : IBotSharpPlugin
{
    public string Id => "f4b367f8-4945-476a-90a7-c3bb8e6d6e49";
    public string Name => "Agent";

    public SettingsMeta Settings =>
        new SettingsMeta("Agent");

    public object GetNewSettingsInstance() =>
         new AgentSettings();

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ILlmProviderSettingService, LlmProviderSettingService>();
        services.AddScoped<IAgentService, AgentService>();

        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<AgentSettings>("Agent");
        });
    }
}
