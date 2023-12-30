using Microsoft.Extensions.Hosting;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ApplicationController : ControllerBase
{
    private readonly IServiceProvider _services;
    public ApplicationController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpGet("/app/shutdown")]
    public IActionResult Restart()
    {
        var app = _services.GetRequiredService<IHostApplicationLifetime>();
        app.StopApplication();
        return Ok();
    }
}
