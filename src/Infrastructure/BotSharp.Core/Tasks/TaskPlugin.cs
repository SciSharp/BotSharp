using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Tasks;
using BotSharp.Core.Tasks.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Tasks;

public class TaskPlugin : IBotSharpPlugin
{
    public string Id => "e1fb196a-8be9-4c3b-91ba-adfab5a359ef";
    public string Name => "Agent Task";
    public string Description => "Define some specific task templates and execute them. It can been used for the fixed business scenarios.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IAgentTaskService, AgentTaskService>();
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("Task", link: "page/task", icon: "bx bx-task", weight: section.Weight + 8));

        return true;
    }
}
