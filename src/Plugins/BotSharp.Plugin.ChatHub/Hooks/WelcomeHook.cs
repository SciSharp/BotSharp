using Microsoft.AspNetCore.SignalR;

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

    #region Events
    private const string RECEIVE_ASSISTANT_MESSAGE = "OnMessageReceivedFromAssistant";
    #endregion

    public WelcomeHook(IServiceProvider services,
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

                var json = JsonSerializer.Serialize(new ChatResponseModel()
                {
                    ConversationId = conversation.Id,
                    MessageId = dialog.MessageId,
                    Text = message.Text,
                    RichContent = richContent,
                    Sender = new UserViewModel()
                    {
                        FirstName = agent.Name,
                        LastName = "",
                        Role = AgentRole.Assistant
                    }
                }, _options.JsonSerializerOptions);

                await Task.Delay(300);

                _storage.Append(conversation.Id, dialog);

                await SendEvent(conversation.Id, json);
            }
        }

        await base.OnUserAgentConnectedInitially(conversation);
    }

    private async Task SendEvent(string conversationId, string json)
    {
        try
        {
            if (_settings.EventDispatchBy == EventDispatchType.Group)
            {
                await _chatHub.Clients.Group(conversationId).SendAsync(RECEIVE_ASSISTANT_MESSAGE, json);
            }
            else
            {
                await _chatHub.Clients.User(_user.Id).SendAsync(RECEIVE_ASSISTANT_MESSAGE, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to send event in {nameof(WelcomeHook)} (conversation id: {conversationId})." +
                $"\r\n{ex.Message}\r\n{ex.InnerException}");
        }
    }
}
