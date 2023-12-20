using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.JsonConverters;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class ChatHubConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly IUserIdentity _user;
    private readonly JsonSerializerOptions _serializerOptions;
    public ChatHubConversationHook(IServiceProvider services,
        IHubContext<SignalRHub> chatHub,
        IUserIdentity user)
    {
        _services = services;
        _chatHub = chatHub;
        _user = user;

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new RichContentJsonConverter(),
                new TemplateMessageJsonConverter(),
            }
        };
    }

    public override async Task OnUserAgentConnectedInitially(Conversation conversation)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(conversation.AgentId);

        // Check if the Welcome template exists.
        var welcomeTemplate = agent.Templates?.FirstOrDefault(x => x.Name == "welcome");
        if (welcomeTemplate != null)
        {
            var richContentService = _services.GetRequiredService<IRichContentService>();
            var messages = richContentService.ConvertToMessages(welcomeTemplate.Content);

            foreach (var message in messages)
            {
                var json = JsonSerializer.Serialize(new ChatResponseModel()
                {
                    ConversationId = conversation.Id,
                    Text = message.Text,
                    RichContent = new RichContent<IRichMessage>(message),
                    Sender = new UserViewModel()
                    {
                        FirstName = "AI",
                        LastName = "Assistant",
                        Role = AgentRole.Assistant
                    }
                }, _serializerOptions);

                await Task.Delay(300);

                await _chatHub.Clients.User(_user.Id).SendAsync("OnMessageReceivedFromAssistant", json);
            }
        }

        await base.OnUserAgentConnectedInitially(conversation);
    }

    public override async Task OnConversationInitialized(Conversation conversation)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var conv = ConversationViewModel.FromSession(conversation);

        var user = await userService.GetUser(conv.User.Id);
        conv.User = UserViewModel.FromUser(user);

        await _chatHub.Clients.User(_user.Id).SendAsync("OnConversationInitFromClient", conv);

        await base.OnConversationInitialized(conversation);
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var sender = await userService.GetMyProfile();

        // Update console conversation UI for CSR
        await _chatHub.Clients.User(_user.Id).SendAsync("OnMessageReceivedFromClient", new ChatResponseModel()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Text = message.Content,
            Sender = UserViewModel.FromUser(sender)
        });

        await base.OnMessageReceived(message);
    }

    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();

        var json = JsonSerializer.Serialize(new ChatResponseModel()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Text = message.Content,
            RichContent = message.RichContent,
            Sender = new UserViewModel()
            {
                FirstName = "AI",
                LastName = "Assistant",
                Role = AgentRole.Assistant
            }
        }, _serializerOptions);
        await _chatHub.Clients.User(_user.Id).SendAsync("OnMessageReceivedFromAssistant", json);

        await base.OnResponseGenerated(message);
    }
}
