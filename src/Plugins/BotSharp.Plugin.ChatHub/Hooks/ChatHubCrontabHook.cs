using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Core.Crontab.Abstraction;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class ChatHubCrontabHook : ICrontabHook
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly ILogger<ChatHubCrontabHook> _logger;
    private readonly IUserIdentity _user;
    private readonly BotSharpOptions _options;
    private readonly ChatHubSettings _settings;

    #region Events
    private const string GENERATE_NOTIFICATION = "OnNotificationGenerated";
    #endregion

    public ChatHubCrontabHook(IServiceProvider services,
        IHubContext<SignalRHub> chatHub,
        ILogger<ChatHubCrontabHook> logger,
        IUserIdentity user,
        BotSharpOptions options,
        ChatHubSettings settings)
    {
        _services = services;
        _chatHub = chatHub;
        _logger = logger;
        _user = user;
        _options = options;
        _settings = settings;
    }

    public async Task OnCronTriggered(CrontabItem item)
    {
        var json = JsonSerializer.Serialize(new ChatResponseModel()
        {
            ConversationId = item.ConversationId,
            MessageId = Guid.NewGuid().ToString(),
            Text = item.ExecutionResult,
            Function = "",
            Sender = new UserViewModel()
            {
                FirstName = "Crontab",
                LastName = "AI",
                Role = AgentRole.Assistant
            }
        }, _options.JsonSerializerOptions);

        await SendEvent(item, json);
    }

    private async Task SendEvent(CrontabItem item, string json)
    {
        try
        {
            await _chatHub.Clients.User(item.UserId).SendAsync(GENERATE_NOTIFICATION, json);
        }
        catch { }
    }
}
