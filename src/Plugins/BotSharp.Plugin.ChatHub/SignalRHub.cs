using BotSharp.Abstraction.Conversations.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BotSharp.Plugin.ChatHub;

[Authorize]
public class SignalRHub : Hub
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IUserIdentity _user;
    private readonly IHttpContextAccessor _context;

    public SignalRHub(IServiceProvider services, 
        ILogger<SignalRHub> logger,
        IUserIdentity user,
        IHttpContextAccessor context)
    {
        _services = services;
        _logger = logger;
        _user = user;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"SignalR Hub: {_user.FirstName} {_user.LastName} ({Context.UserIdentifier}) connected in {Context.ConnectionId} [{DateTime.Now}]");

        var hooks = _services.GetServices<IConversationHook>();
        var convService = _services.GetRequiredService<IConversationService>();
        _context.HttpContext.Request.Query.TryGetValue("conversationId", out var conversationId);
        var conv = await convService.GetConversation(conversationId);

        foreach (var hook in hooks)
        {
            // Check if user connected with agent is the first time.
            if (!conv.Dialogs.Any())
            {
                await hook.OnUserAgentConnectedInitially(conv);
            }
        }

        await base.OnConnectedAsync();
    }
}
