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
        return loader.GetPlugins();
    }
}
