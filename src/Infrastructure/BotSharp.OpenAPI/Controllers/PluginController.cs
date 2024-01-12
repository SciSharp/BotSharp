using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Core.Plugins;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class PluginController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly PluginSettings _settings;

    public PluginController(IServiceProvider services, PluginSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    [HttpGet("/plugins")]
    public List<PluginDef> GetPlugins()
    {
        var loader = _services.GetRequiredService<PluginLoader>();
        return loader.GetPlugins(_services);
    }

    [HttpGet("/plugin/menu")]
    public List<PluginMenuDef> GetPluginMenu()
    {
        var menus = new List<PluginMenuDef>
        {
            new PluginMenuDef("Dashboard", link: "/page/dashboard", icon: "bx bx-home-circle", weight: 1),
            new PluginMenuDef("Agent", isHeader: true, weight: 5),
            new PluginMenuDef("Router", link: "/page/agent/router", icon: "bx bx-map-pin", weight: 6),
            // new PluginMenuDef("Evaluator", link: "/page/agent/evaluator", icon: "bx bx-task", weight: 7),
            new PluginMenuDef("Agents", link: "/page/agent", icon: "bx bx-bot", weight: 8),
            new PluginMenuDef("Conversation", isHeader: true, weight: 15),
            new PluginMenuDef("Conversations", link: "/page/conversation", icon: "bx bx-conversation", weight: 16),
            new PluginMenuDef("System", isHeader: true, weight: 30),
            new PluginMenuDef("Plugins", link: "/page/plugin", icon: "bx bx-plug", weight: 31),
            new PluginMenuDef("Settings", link: "/page/setting", icon: "bx bx-cog", weight: 32),
        };
        var loader = _services.GetRequiredService<PluginLoader>();
        foreach (var plugin in loader.GetPlugins(_services))
        {
            if (!plugin.Enabled)
            {
                continue;
            }
            menus.AddRange(plugin.Menus);
        }
        menus = menus.OrderBy(x => x.Weight).ToList();
        return menus;
    }

    [HttpPost("/plugin/{id}/enable")]
    public PluginDef EnablePlugin([FromRoute] string id)
    {
        var loader = _services.GetRequiredService<PluginLoader>();
        return loader.UpdatePluginStatus(_services, id, true);
    }

    [HttpPost("/plugin/{id}/disable")]
    public PluginDef DisablePluginStats([FromRoute] string id)
    {
        var loader = _services.GetRequiredService<PluginLoader>();
        return loader.UpdatePluginStatus(_services, id, false);
    }
}
