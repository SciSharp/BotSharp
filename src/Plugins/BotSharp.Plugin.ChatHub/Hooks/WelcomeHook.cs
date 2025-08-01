using BotSharp.Abstraction.Conversations.Dtos;
using BotSharp.Abstraction.Conversations.Enums;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class WelcomeHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly ILogger<WelcomeHook> _logger;
    private readonly IUserIdentity _user;
    private readonly IConversationStorage _storage;
    private readonly BotSharpOptions _options;
    private readonly ChatHubSettings _settings;

    public WelcomeHook(
        IServiceProvider services,
        IHubContext<SignalRHub> chatHub,
        ILogger<WelcomeHook> logger,
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

    public override async Task OnUserAgentConnectedInitially(Conversation conversation)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.GetAgent(conversation.AgentId);

        // Check if the Welcome template exists.
        var welcomeTemplate = agent?.Templates?.FirstOrDefault(x => x.Name == ".welcome");
        if (welcomeTemplate != null)
        {
            // Render template
            var templating = _services.GetRequiredService<ITemplateRender>();
            var user = _services.GetRequiredService<IUserIdentity>();
            var content = templating.Render(welcomeTemplate.Content, new Dictionary<string, object>
            {
                { "user",  user }
            });
            var richContentService = _services.GetRequiredService<IRichContentService>();
            var messages = richContentService.ConvertToMessages(content);
            var guid = Guid.NewGuid().ToString();

            foreach (var message in messages)
            {
                var richContent = new RichContent<IRichMessage>(message);
                var dialog = new RoleDialogModel(AgentRole.Assistant, message.Text)
                {
                    MessageId = guid,
                    CurrentAgentId = agent.Id,
                    RichContent = richContent
                };

                var data = new ChatResponseDto()
                {
                    ConversationId = conversation.Id,
                    MessageId = dialog.MessageId,
                    Text = message.Text,
                    RichContent = richContent,
                    Sender = new()
                    {
                        FirstName = agent.Name,
                        LastName = "",
                        Role = AgentRole.Assistant
                    }
                };

                await Task.Delay(300);
                _storage.Append(conversation.Id, dialog);
                await SendEvent(ChatEvent.OnMessageReceivedFromAssistant, conversation.Id, data);
            }
        }

        await base.OnUserAgentConnectedInitially(conversation);
    }

    private async Task SendEvent<T>(string @event, string conversationId, T data, [CallerMemberName] string callerName = "")
    {
        var user = _services.GetRequiredService<IUserIdentity>();
        await EventEmitter.SendChatEvent(_services, _logger, @event, conversationId, user?.Id, data, nameof(WelcomeHook), callerName);
    }
}
