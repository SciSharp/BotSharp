using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Core.Crontab.Abstraction;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class ChatHubCrontabHook : ICrontabHook
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly IUserIdentity _user;
    private readonly IConversationStorage _storage;
    private readonly BotSharpOptions _options;
    private readonly ChatHubSettings _settings;

    #region Events
    private const string GENERATE_NOTIFICATION = "OnNotificationGenerated";
    #endregion

    public ChatHubCrontabHook(IServiceProvider services,
        IHubContext<SignalRHub> chatHub,
        IUserIdentity user,
        IConversationStorage storage,
        BotSharpOptions options,
        ChatHubSettings settings)
    {
        _services = services;
        _chatHub = chatHub;
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

        if (_settings.EventDispatchBy == EventDispatchType.Group)
        {
            await _chatHub.Clients.Group(item.ConversationId).SendAsync(GENERATE_NOTIFICATION, json);
        }
        else
        {
            await _chatHub.Clients.User(item.UserId).SendAsync(GENERATE_NOTIFICATION, json);
        }
    }
}
