namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class FileController : ControllerBase
{
    private readonly IServiceProvider _services;

    public FileController(IServiceProvider services)
    {
        _services = services;
    }
}
