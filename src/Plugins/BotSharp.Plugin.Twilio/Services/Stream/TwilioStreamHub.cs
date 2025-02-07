using BotSharp.Plugin.Twilio.Models.Stream;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services.Stream;

public class TwilioStreamHub : Hub
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _context;

    public TwilioStreamHub(IServiceProvider services,
        ILogger<TwilioStreamHub> logger,
        IHttpContextAccessor context)
    {
        _services = services;
        _logger = logger;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Twilio Stream Hub: {Context.ConnectionId} connected.");

        await base.OnConnectedAsync();
    }

    public async Task<string> OnMessageReceived(StreamEventMediaResponse media)
    {
        return null;
    }
}
