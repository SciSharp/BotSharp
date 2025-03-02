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
    private readonly IConversationStorage _storage;
    private readonly BotSharpOptions _options;
    private readonly ChatHubSettings _settings;

    #region Events
    private const string GENERATE_NOTIFICATION = "OnNotificationGenerated";
    #endregion

    public ChatHubCrontabHook(IServiceProvider services,
        IHubContext<SignalRHub> chatHub,
        ILogger<ChatHubCrontabHook> logger,
        IUserIdentity user,
        IConversationStorage storage,
        BotSharpOptions options,
        ChatHubSettings settings)
    {
        _services = services;
        _chatHub = chatHub;
        _logger = logger;
        _user = user;
        _storage = storage;
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
            if (_settings.EventDispatchBy == EventDispatchType.Group)
            {
                await _chatHub.Clients.Group(item.ConversationId).SendAsync(GENERATE_NOTIFICATION, json);
            }
            else
            {
                await _chatHub.Clients.User(item.UserId).SendAsync(GENERATE_NOTIFICATION, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to send event in {nameof(ChatHubCrontabHook)} (conversation id: {item.ConversationId})." +
                $"\r\n{ex.Message}\r\n{ex.InnerException}");
        }
    }
}
