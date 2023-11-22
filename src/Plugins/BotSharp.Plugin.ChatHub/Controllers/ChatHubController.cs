using BotSharp.Abstraction.ApiAdapters;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Controllers;

[Authorize]
[ApiController]
public class ChatHubController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly IUserIdentity _user;

    public ChatHubController(IServiceProvider services,
        ILogger<ChatHubController> logger,
        IHubContext<SignalRHub> chatHub,
        IUserIdentity user)
    {
        _services = services;
        _logger = logger;
        _chatHub = chatHub;
        _user = user;
    }
}
