using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Core.Plugins;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class PluginController(IServiceProvider services, IUserIdentity user, PluginSettings settings) : ControllerBase
{
    private readonly IUserIdentity _user = user;

    [HttpGet("/plugins")]
    public async Task<PagedItems<PluginDef>> GetPlugins([FromQuery] PluginFilter filter)
    {
        var isValid = await IsValidUser();
        if (!isValid)
        {
            return new PagedItems<PluginDef>();
        }

        var loader = services.GetRequiredService<PluginLoader>();
        return loader.GetPagedPlugins(services, filter);
    }

    [HttpGet("/plugin/menu")]
    public async Task<List<PluginMenuDef>> GetPluginMenu()
    {
        List<PluginMenuDef> menu =
        [
            new PluginMenuDef("Apps", weight: 5)
            {
                IsHeader = true,
            },
            new PluginMenuDef("System", weight: 30)
            { 
                IsHeader = true,
                Roles = [UserRole.Root, UserRole.Admin]
            },
            new PluginMenuDef("Plugins", link: "page/plugin", icon: "bx bx-plug", weight: 31)
            {
                Roles = [UserRole.Root, UserRole.Admin]
            },
            new PluginMenuDef("Settings", link: "page/setting", icon: "bx bx-cog", weight: 32)
            {
                Roles = [UserRole.Root, UserRole.Admin]
            },
            new PluginMenuDef("Roles", link: "page/roles", icon: "bx bx-group", weight: 33)
            {
                Roles = [UserRole.Root, UserRole.Admin]
            },
            new PluginMenuDef("Users", link: "page/users", icon: "bx bx-user", weight: 34)
            {
                Roles = [UserRole.Root, UserRole.Admin]
            }
        ];

        var loader = services.GetRequiredService<PluginLoader>();
        foreach (var plugin in loader.GetPlugins(services))
        {
            if (!plugin.Enabled)
            {
                continue;
            }
            plugin.Module.AttachMenu(menu);
        }

        var userService = services.GetRequiredService<IUserService>();
        var user = await userService.GetUser(_user.Id);
        menu = loader.GetPluginMenuByRoles(menu, user?.Role);
        menu = menu.OrderBy(x => x.Weight).ToList();
        return menu;
    }

    [HttpPost("/plugin/{id}/install")]
    public PluginDef InstallPlugin([FromRoute] string id)
    {
        var loader = services.GetRequiredService<PluginLoader>();
        return loader.UpdatePluginStatus(services, id, true);
    }

    [HttpPost("/plugin/{id}/remove")]
    public PluginDef RemovePluginStats([FromRoute] string id)
    {
        var loader = services.GetRequiredService<PluginLoader>();
        return loader.UpdatePluginStatus(services, id, false);
    }

    private async Task<bool> IsValidUser()
    {
        var userService = services.GetRequiredService<IUserService>();
        var (isAdmin, _) = await userService.IsAdminUser(_user.Id);
        return isAdmin;
    }
}
