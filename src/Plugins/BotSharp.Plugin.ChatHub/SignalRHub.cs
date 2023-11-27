using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub;

public class SignalRHub : Hub
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IUserIdentity _user;

    public SignalRHub(IServiceProvider services, 
        ILogger<SignalRHub> logger,
        IUserIdentity user)
    {
        _services = services;
        _logger = logger;
        _user = user;
    }

    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"SignalR Hub: {Context.UserIdentifier} connected in {Context.ConnectionId} [{DateTime.Now}]");
        return base.OnConnectedAsync();
    }
}
