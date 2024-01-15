using BotSharp.Abstraction.Settings;
using BotSharp.Core.Plugins;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class SettingController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly ISettingService _settingService;

    public SettingController(IServiceProvider services,
        ISettingService settingService)
    {
        _services = services;
        _settingService = settingService;
    }

    [HttpGet("/settings")]
    public List<string> GetSettings()
    {
        var pluginService = _services.GetRequiredService<PluginLoader>();
        var plugins = pluginService.GetPlugins(_services);

        return plugins.Where(x => x.Module.Settings != null && !string.IsNullOrEmpty(x.Module.Settings.Name))
            .Select(x => x.Module.Settings.Name)
            .OrderBy(x => x)
            .ToList();
    }

    [HttpGet("/setting/{id}")]
    public object GetSettingDetail([FromRoute] string id)
    {
        return _settingService.GetDetail(id, mask: true);
    }
}
