using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Options;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class ChatHubConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly IUserIdentity _user;
    private readonly BotSharpOptions _options; 
    public ChatHubConversationHook(IServiceProvider services,
        IHubContext<SignalRHub> chatHub,
        BotSharpOptions options,
        IUserIdentity user)
    {
        _services = services;
        _chatHub = chatHub;
        _user = user;
        _options = options;
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
            Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
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

    public override async Task OnFunctionExecuting(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();

        await _chatHub.Clients.User(_user.Id).SendAsync("OnSenderActionGenerated", new ConversationSenderActionModel
        {
            ConversationId = conv.ConversationId,
            SenderAction = SenderActionEnum.TypingOn,
            Indication = message.Indication
        });

        await base.OnFunctionExecuting(message);
    }

    public override async Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg)
    {
        await this.OnMessageReceived(message);
    }

    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var json = JsonSerializer.Serialize(new ChatResponseModel()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
            Function = message.FunctionName,
            RichContent = message.SecondaryRichContent ?? message.RichContent,
            Data = message.Data,
            Sender = new UserViewModel()
            {
                FirstName = "AI",
                LastName = "Assistant",
                Role = AgentRole.Assistant
            }
        }, _options.JsonSerializerOptions);

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
