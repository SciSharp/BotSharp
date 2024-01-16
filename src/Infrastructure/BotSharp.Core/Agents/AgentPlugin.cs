using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins.Models;
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
        services.AddScoped<ILlmProviderService, LlmProviderService>();
        services.AddScoped<IAgentService, AgentService>();

        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<AgentSettings>("Agent");
        });
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("Agent", icon: "bx bx-bot", weight: section.Weight + 1)
        {
            SubMenu = new List<PluginMenuDef>
            {
                new PluginMenuDef("Router", link: "/page/agent/router"), // icon: "bx bx-map-pin"
                new PluginMenuDef("Evaluator", link: "/page/agent/evaluator"), // icon: "bx bx-task"
                new PluginMenuDef("Agents", link: "/page/agent"), // icon: "bx bx-bot"
            }
        });

        return true;
    }
}
