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

    #region Event
    private const string INIT_CLIENT_CONVERSATION = "OnConversationInitFromClient";
    private const string RECEIVE_CLIENT_MESSAGE = "OnMessageReceivedFromClient";
    private const string RECEIVE_ASSISTANT_MESSAGE = "OnMessageReceivedFromAssistant";
    private const string GENERATE_SENDER_ACTION = "OnSenderActionGenerated";
    private const string DELETE_MESSAGE = "OnMessageDeleted";
    #endregion

    public ChatHubConversationHook(
        IServiceProvider services,
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

        await InitClientConversation(conv);
        await base.OnConversationInitialized(conversation);
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var sender = await userService.GetMyProfile();

        // Update console conversation UI for CSR
        
        var model = new ChatResponseModel()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
            Sender = UserViewModel.FromUser(sender)
        };
        await ReceiveClientMessage(model);

        // Send typing-on to client
        var action = new ConversationSenderActionModel
        {
            ConversationId = conv.ConversationId,
            SenderAction = SenderActionEnum.TypingOn
        };
        await GenerateSenderAction(action);
        await base.OnMessageReceived(message);
    }

    public override async Task OnFunctionExecuting(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var action = new ConversationSenderActionModel
        {
            ConversationId = conv.ConversationId,
            SenderAction = SenderActionEnum.TypingOn,
            Indication = message.Indication
        };
        await GenerateSenderAction(action);
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
        var action = new ConversationSenderActionModel
        {
            ConversationId = conv.ConversationId,
            SenderAction = SenderActionEnum.TypingOff
        };

        await GenerateSenderAction(action);
        await ReceiveAssistantMessage(json);
        await base.OnResponseGenerated(message);
    }

    public override async Task OnMessageDeleted(string conversationId, string messageId)
    {
        var model = new ChatResponseModel
        {
            ConversationId = conversationId,
            MessageId = messageId
        };
        await DeleteMessage(model);
        await base.OnMessageDeleted(conversationId, messageId);
    }

    #region Private methods
    private async Task InitClientConversation(ConversationViewModel conversation)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(INIT_CLIENT_CONVERSATION, conversation);
    }

    private async Task ReceiveClientMessage(ChatResponseModel model)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(RECEIVE_CLIENT_MESSAGE, model);
    }

    private async Task ReceiveAssistantMessage(string? json)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(RECEIVE_ASSISTANT_MESSAGE, json);
    }

    private async Task GenerateSenderAction(ConversationSenderActionModel action)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(GENERATE_SENDER_ACTION, action);
    }

    private async Task DeleteMessage(ChatResponseModel model)
    {
        await _chatHub.Clients.User(_user.Id).SendAsync(DELETE_MESSAGE, model);
    }
    #endregion
}
