using Microsoft.AspNetCore.SignalR;

namespace BotSharp.OpenAPI;

public class ChatHub : Hub
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IUserIdentity _user;

    public ChatHub(IServiceProvider services, 
        ILogger<ChatHub> logger,
        IUserIdentity user)
    {
        _services = services;
        _logger = logger;
        _user = user;
    }

    public async Task OnMessageReceivedFromClient(string message)
    {
        // await Clients.User(_user.Id).SendAsync("ReceiveMessage", message);
    }

    /// <summary>
    /// Received message from client
    /// </summary>
    /// <param name="user"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task SendMessage(string user, string message)
    {
        // await Clients.User(_user.Id).SendAsync("ReceiveMessage", message);
    }

    public async Task SendMessageToCaller(string user, string message)
        => await Clients.Caller.SendAsync("ReceiveMessage", user, message);

    public async Task SendMessageToGroup(string user, string message)
        => await Clients.Group("SignalR Users").SendAsync("ReceiveMessage", user, message);

    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"ChatHub: {""} connected in {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }
}
