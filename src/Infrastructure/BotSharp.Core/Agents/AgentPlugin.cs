using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Settings;
using BotSharp.Abstraction.Users.Enums;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Agents;

public class AgentPlugin : IBotSharpPlugin
{
    public string Id => "f4b367f8-4945-476a-90a7-c3bb8e6d6e49";
    public string Name => "Agent";
    public string Description => "A container of agent profile includes instruction, functions, examples and templates/ response templates.";

    public SettingsMeta Settings =>
        new SettingsMeta("Agent");

    public string[] AgentIds => new string[]
    {
        "01fcc3e5-9af7-49e6-ad7a-a760bd12dc4a",
        "01e2fc5c-2c89-4ec7-8470-7688608b496c",
        "01dcc3e5-0af7-49e6-ad7a-a760bd12dc4b"
    };

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
                new PluginMenuDef("Routing", link: "page/agent/router"), // icon: "bx bx-map-pin"
                new PluginMenuDef("Evaluating", link: "page/agent/evaluator") { Roles = new List<string> { UserRole.Admin } }, // icon: "bx bx-task"
                new PluginMenuDef("Agents", link: "page/agent"), // icon: "bx bx-bot"
            }
        });

        return true;
    }
}
