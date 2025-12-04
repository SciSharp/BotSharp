namespace BotSharp.Plugin.Membase.Controllers;

[Authorize]
[ApiController]
public class MembaseController : ControllerBase
{
    private readonly IUserIdentity _user;
    private readonly IServiceProvider _services;

    public MembaseController(
        IUserIdentity user,
        IServiceProvider services)
    {
        _user = user;
        _services = services;
    }
}
