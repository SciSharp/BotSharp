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
    public PagedItems<PluginDef> GetPlugins([FromQuery] PluginFilter filter)
    {
        var loader = _services.GetRequiredService<PluginLoader>();
        return loader.GetPagedPlugins(_services, filter);
    }

    [HttpGet("/plugin/menu")]
    public List<PluginMenuDef> GetPluginMenu()
    {
        var menu = new List<PluginMenuDef>
        {
            new PluginMenuDef("Dashboard", link: "/page/dashboard", icon: "bx bx-home-circle", weight: 1),
            new PluginMenuDef("Apps", weight: 5)
            {
                IsHeader = true,
            },
            new PluginMenuDef("System", weight: 30)
            {
                IsHeader = true
            },
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
            plugin.Module.AttachMenu(menu);
        }
        menu = menu.OrderBy(x => x.Weight).ToList();
        return menu;
    }

    [HttpPost("/plugin/{id}/install")]
    public PluginDef InstallPlugin([FromRoute] string id)
    {
        var loader = _services.GetRequiredService<PluginLoader>();
        return loader.UpdatePluginStatus(_services, id, true);
    }

    [HttpPost("/plugin/{id}/remove")]
    public PluginDef RemovePluginStats([FromRoute] string id)
    {
        var loader = _services.GetRequiredService<PluginLoader>();
        return loader.UpdatePluginStatus(_services, id, false);
    }
}
