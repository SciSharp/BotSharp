using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class RouterController : ControllerBase
{
    private readonly IServiceProvider _services;
    public RouterController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpGet("/router/settings")]
    public RoutingSettings GetSettings()
    {
        var settings = _services.GetRequiredService<RoutingSettings>();
        return settings;
    }
}
