using BotSharp.Abstraction.Conversations.Dtos;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Routing.Enums;
using BotSharp.Abstraction.SideCar;
using BotSharp.Abstraction.Users.Dtos;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class ChatHubConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly ILogger<ChatHubConversationHook> _logger;
    private readonly IUserIdentity _user;
    private readonly BotSharpOptions _options;
    private readonly ChatHubSettings _settings;

    public ChatHubConversationHook(
        IServiceProvider services,
        IHubContext<SignalRHub> chatHub,
        ILogger<ChatHubConversationHook> logger,
        BotSharpOptions options,
        ChatHubSettings settings,
        IUserIdentity user)
    {
        _services = services;
        _chatHub = chatHub;
        _logger = logger;
        _user = user;
        _options = options;
        _settings = settings;
        Priority = -1; // Make sure this hook is the top one.
    }

    public override async Task OnConversationInitialized(Conversation conversation)
    {
        if (!AllowSendingMessage()) return;

        var userService = _services.GetRequiredService<IUserService>();
        var conv = ConversationDto.FromSession(conversation);

        var user = await userService.GetUser(conv.User.Id);
        conv.User = UserDto.FromUser(user);

        //await InitClientConversation(conv.Id, conv);
        await SendEvent(ChatEvent.OnConversationInitFromClient, conv.Id, conv);
        await base.OnConversationInitialized(conversation);
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        if (!AllowSendingMessage()) return;

        var conv = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var sender = await userService.GetMyProfile();

        // Update console conversation UI for CSR
        var model = new ChatResponseDto()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Payload = message.Payload,
            Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
            Sender = UserDto.FromUser(sender)
        };
        await SendEvent(ChatEvent.OnMessageReceivedFromClient, conv.ConversationId, model);

        // Send typing-on to client
        var action = new ConversationSenderActionModel
        {
            ConversationId = conv.ConversationId,
            SenderAction = SenderActionEnum.TypingOn
        };
        await SendEvent(ChatEvent.OnSenderActionGenerated, conv.ConversationId, action);
        await base.OnMessageReceived(message);
    }

    public override async Task OnFunctionExecuting(RoleDialogModel message, string from = InvokeSource.Manual)
    {
        await base.OnFunctionExecuting(message, from: from);
    }

    public override async Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg)
    {
        await this.OnMessageReceived(message);
    }

    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        if (!AllowSendingMessage()) return;

        var conv = _services.GetRequiredService<IConversationService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var data = new ChatResponseDto()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
            Function = message.FunctionName,
            RichContent = message.SecondaryRichContent ?? message.RichContent,
            Data = message.Data,
            States = state.GetStates(),
            IsStreaming = message.IsStreaming,
            Sender = new()
            {
                FirstName = "AI",
                LastName = "Assistant",
                Role = AgentRole.Assistant
            }
        };

        // Send typing-off to client
        if (!message.IsStreaming)
        {
            var action = new ConversationSenderActionModel
            {
                ConversationId = conv.ConversationId,
                SenderAction = SenderActionEnum.TypingOff
            };
            await SendEvent(ChatEvent.OnSenderActionGenerated, conv.ConversationId, action);
        }
        
        await SendEvent(ChatEvent.OnMessageReceivedFromAssistant, conv.ConversationId, data);
        await base.OnResponseGenerated(message);
    }


    public override async Task OnNotificationGenerated(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var data = new ChatResponseDto()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
            Function = message.FunctionName,
            RichContent = message.SecondaryRichContent ?? message.RichContent,
            Data = message.Data,
            Sender = new()
            {
                FirstName = "AI",
                LastName = "Assistant",
                Role = AgentRole.Assistant
            }
        };

        await SendEvent(ChatEvent.OnNotificationGenerated, conv.ConversationId, data);
        await base.OnNotificationGenerated(message);
    }


    public override async Task OnMessageDeleted(string conversationId, string messageId)
    {
        var model = new ChatResponseDto
        {
            ConversationId = conversationId,
            MessageId = messageId
        };

        await SendEvent(ChatEvent.OnMessageDeleted, conversationId, model);
        await base.OnMessageDeleted(conversationId, messageId);
    }

    #region Private methods
    private bool AllowSendingMessage()
    {
        var sidecar = _services.GetService<IConversationSideCar>();
        return sidecar == null || !sidecar.IsEnabled;
    }

    private async Task SendEvent<T>(string @event, string conversationId, T data, [CallerMemberName] string callerName = "")
    {
        var user = _services.GetRequiredService<IUserIdentity>();
        await ChatHubHelper.SendChatEvent(_services, _logger, @event, conversationId, user?.Id, data, nameof(ChatHubConversationHook), callerName);
    }
    #endregion
}
