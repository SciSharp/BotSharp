using BotSharp.Abstraction.Conversations.Dtos;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
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

    public ChatHubCrontabHook(
        IServiceProvider services,
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
        var data = new ChatResponseDto()
        {
            ConversationId = item.ConversationId,
            MessageId = Guid.NewGuid().ToString(),
            Text = item.ExecutionResult,
            Function = "",
            Sender = new()
            {
                FirstName = "Crontab",
                LastName = "AI",
                Role = AgentRole.Assistant
            }
        };

        await SendEvent(item, data);
    }

    private async Task SendEvent(CrontabItem item, ChatResponseDto data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
            await _chatHub.Clients.User(item.UserId).SendAsync(ChatEvent.OnNotificationGenerated, json);
        }
        catch { }
    }
}
