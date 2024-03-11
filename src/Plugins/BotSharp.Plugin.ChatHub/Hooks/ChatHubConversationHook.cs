using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Messaging.JsonConverters;
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

        // Send typing-on to client
        await _chatHub.Clients.User(_user.Id).SendAsync("OnSenderActionGenerated", new ConversationSenderActionModel
        {
            ConversationId = conv.ConversationId,
            SenderAction = SenderActionEnum.TypingOn
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
            Function = message.FunctionName,
            RichContent = message.RichContent,
            Data = message.Data,
            Sender = new UserViewModel()
            {
                FirstName = "AI",
                LastName = "Assistant",
                Role = AgentRole.Assistant
            }
        }, _serializerOptions);

        // Send typing-off to client
        await _chatHub.Clients.User(_user.Id).SendAsync("OnSenderActionGenerated", new ConversationSenderActionModel
        {
            ConversationId = conv.ConversationId,
            SenderAction = SenderActionEnum.TypingOff
        });
        await _chatHub.Clients.User(_user.Id).SendAsync("OnMessageReceivedFromAssistant", json);

        await base.OnResponseGenerated(message);
    }

    public override async Task OnMessageDeleted(string conversationId, string messageId)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync("OnMessageDeleted", new ChatResponseModel
        {
            ConversationId = conversationId,
            MessageId = messageId
        });
        await base.OnMessageDeleted(conversationId, messageId);
    }
}
