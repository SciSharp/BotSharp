using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class WelcomeHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly IUserIdentity _user;
    private readonly IConversationStorage _storage;
    private readonly BotSharpOptions _options;

    public WelcomeHook(IServiceProvider services,
        IHubContext<SignalRHub> chatHub,
        IUserIdentity user,
        IConversationStorage storage,
        BotSharpOptions options)
    {
        _services = services;
        _chatHub = chatHub;
        _user = user;
        _storage = storage;
        _options = options;
    }

    public override async Task OnUserAgentConnectedInitially(Conversation conversation)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(conversation.AgentId);

        // Check if the Welcome template exists.
        var welcomeTemplate = agent.Templates?.FirstOrDefault(x => x.Name == ".welcome");
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

            foreach (var message in messages)
            {
                var richContent = new RichContent<IRichMessage>(message)
                {
                };

                var json = JsonSerializer.Serialize(new ChatResponseModel()
                {
                    ConversationId = conversation.Id,
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

                _storage.Append(conversation.Id, new RoleDialogModel(AgentRole.Assistant, message.Text)
                {
                    MessageId = conversation.Id,
                    CurrentAgentId = agent.Id,
                    RichContent = richContent
                });

                await _chatHub.Clients.User(_user.Id).SendAsync("OnMessageReceivedFromAssistant", json);
            }
        }

        await base.OnUserAgentConnectedInitially(conversation);
    }
}
