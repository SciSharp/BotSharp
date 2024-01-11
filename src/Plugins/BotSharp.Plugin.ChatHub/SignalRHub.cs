using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

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
        _logger.LogInformation($"SignalR Hub: {_user.FirstName} {_user.LastName} ({Context.User.Identity.Name}) connected in {Context.ConnectionId}");

        var hooks = _services.GetServices<IConversationHook>();
        var convService = _services.GetRequiredService<IConversationService>();
        _context.HttpContext.Request.Query.TryGetValue("conversationId", out var conversationId);

        if (!string.IsNullOrEmpty(conversationId))
        {
            _logger.LogInformation($"Connection {Context.ConnectionId} is with conversation {conversationId}");
            var conv = await convService.GetConversation(conversationId);
            if (conv != null)
            {
                foreach (var hook in hooks)
                {
                    // Check if user connected with agent is the first time.
                    if (!conv.Dialogs.Any())
                    {
                        await hook.OnUserAgentConnectedInitially(conv);
                    }
                }
            }
        }

        await base.OnConnectedAsync();
    }
}
