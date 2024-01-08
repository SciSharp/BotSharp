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

    [AllowAnonymous]
    [HttpGet("/app/health")]
    public IActionResult Health()
    {
        return Ok(new { });
    }

    [HttpGet("/app/shutdown")]
    public IActionResult Restart()
    {
        var app = _services.GetRequiredService<IHostApplicationLifetime>();
        app.StopApplication();
        return Ok();
    }
}
