using BotSharp.Abstraction.Conversations.Dtos;
using BotSharp.Abstraction.Observables.Models;
using BotSharp.Abstraction.SideCar;
using BotSharp.Abstraction.Users.Dtos;
using BotSharp.Plugin.ChatHub.Hooks;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Observers;

public class ChatHubObserver : IObserver<HubObserveData>
{
    private readonly ILogger _logger;
    private IServiceProvider _services;
    private IUserIdentity _user;

    private const string RECEIVE_CLIENT_MESSAGE = "OnMessageReceivedFromClient";
    private const string GENERATE_SENDER_ACTION = "OnSenderActionGenerated";

    public ChatHubObserver(ILogger logger)
    {
        _logger = logger;
    }

    public void OnCompleted()
    {
        _logger.LogInformation($"{nameof(ChatHubObserver)} receives complete notification.");
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, $"{nameof(ChatHubObserver)} receives error notification: {error.Message}");
    }

    public void OnNext(HubObserveData value)
    {
        _services = value.ServiceProvider;
        _user = _services.GetRequiredService<IUserIdentity>();
        
        ReceiveMessage(value.Data).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task ReceiveMessage(RoleDialogModel message)
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
        await ReceiveClientMessage(conv.ConversationId, model);

        // Send typing-on to client
        var action = new ConversationSenderActionModel
        {
            ConversationId = conv.ConversationId,
            SenderAction = SenderActionEnum.TypingOn
        };

        await GenerateSenderAction(conv.ConversationId, action);
    }

    private async Task ReceiveClientMessage(string conversationId, ChatResponseDto model)
    {
        try
        {
            var settings = _services.GetRequiredService<ChatHubSettings>();
            var chatHub = _services.GetRequiredService<IHubContext<SignalRHub>>();

            if (settings.EventDispatchBy == EventDispatchType.Group)
            {
                await chatHub.Clients.Group(conversationId).SendAsync(RECEIVE_CLIENT_MESSAGE, model);
            }
            else
            {
                await chatHub.Clients.User(_user.Id).SendAsync(RECEIVE_CLIENT_MESSAGE, model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to receive assistant message in {nameof(ChatHubConversationHook)} (conversation id: {conversationId})");
        }
    }

    private bool AllowSendingMessage()
    {
        var sidecar = _services.GetService<IConversationSideCar>();
        return sidecar == null || !sidecar.IsEnabled();
    }

    private async Task GenerateSenderAction(string conversationId, ConversationSenderActionModel action)
    {
        try
        {
            var settings = _services.GetRequiredService<ChatHubSettings>();
            var chatHub = _services.GetRequiredService<IHubContext<SignalRHub>>();
            if (settings.EventDispatchBy == EventDispatchType.Group)
            {
                await chatHub.Clients.Group(conversationId).SendAsync(GENERATE_SENDER_ACTION, action);
            }
            else
            {
                await chatHub.Clients.User(_user.Id).SendAsync(GENERATE_SENDER_ACTION, action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to generate sender action in {nameof(ChatHubConversationHook)} (conversation id: {conversationId})");
        }
    }
}
