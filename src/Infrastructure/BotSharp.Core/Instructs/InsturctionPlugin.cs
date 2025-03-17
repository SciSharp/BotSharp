using BotSharp.Abstraction.Instructs.Settings;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Settings;
using BotSharp.Core.Instructs.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Instructs;

public class InsturctionPlugin : IBotSharpPlugin
{
    public string Id => "8189e133-819c-4505-9f82-84f793bc1be0";
    public string Name => "Instruction";
    public string Description => "Handle agent instruction request";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<InstructionSettings>("Instruction");
        });

        services.AddScoped<IAgentUtilityHook, InstructUtilityHook>();
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("Instruction", icon: "bx bx-book-content", weight: section.Weight + 5)
        {
            SubMenu = new List<PluginMenuDef>
            {
                new PluginMenuDef("Testing", link: "page/instruction/testing"),
                new PluginMenuDef("Log", link: "page/instruction/log")
            }
        });

        return true;
    }
}
