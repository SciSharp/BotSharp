using BotSharp.Abstraction.Conversations.Dtos;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.SideCar;
using BotSharp.Plugin.ChatHub.Hooks;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Observers;

public class ChatHubObserver : IObserver<HubObserveData>
{
    private readonly ILogger _logger;
    private IServiceProvider _services;

    public ChatHubObserver(ILogger logger)
    {
        _logger = logger;
    }

    public void OnCompleted()
    {
        _logger.LogWarning($"{nameof(ChatHubObserver)} receives complete notification.");
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, $"{nameof(ChatHubObserver)} receives error notification: {error.Message}");
    }

    public void OnNext(HubObserveData value)
    {
        _services = value.ServiceProvider;

        if (!AllowSendingMessage()) return;

        var message = value.Data;
        var model = new ChatResponseDto();
        var action = new ConversationSenderActionModel();
        var conv = _services.GetRequiredService<IConversationService>();

        switch (value.EventName)
        {
            case ChatEvent.BeforeReceiveLlmStreamMessage:
                model = new ChatResponseDto()
                {
                    ConversationId = conv.ConversationId,
                    MessageId = message.MessageId,
                    Text = string.Empty,
                    Sender = new()
                    {
                        FirstName = "AI",
                        LastName = "Assistant",
                        Role = AgentRole.Assistant
                    }
                };

                action = new ConversationSenderActionModel
                {
                    ConversationId = conv.ConversationId,
                    SenderAction = SenderActionEnum.TypingOn
                };

                GenerateSenderAction(conv.ConversationId, action);
                break;
            case ChatEvent.OnReceiveLlmStreamMessage:
                model = new ChatResponseDto()
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
                break;
            case ChatEvent.AfterReceiveLlmStreamMessage:
                model = new ChatResponseDto()
                {
                    ConversationId = conv.ConversationId,
                    MessageId = message.MessageId,
                    Text = message.Content,
                    Sender = new()
                    {
                        FirstName = "AI",
                        LastName = "Assistant",
                        Role = AgentRole.Assistant
                    }
                };

                action = new ConversationSenderActionModel
                {
                    ConversationId = conv.ConversationId,
                    SenderAction = SenderActionEnum.TypingOff
                };

                GenerateSenderAction(conv.ConversationId, action);
                break;
            case ChatEvent.OnIndicationReceived:
                model = new ChatResponseDto
                {
                    ConversationId = conv.ConversationId,
                    MessageId = message.MessageId,
                    Indication = message.Indication,
                    Sender = new()
                    {
                        FirstName = "AI",
                        LastName = "Assistant",
                        Role = AgentRole.Assistant
                    }
                };
                break;
        }

        OnReceiveAssistantMessage(value.EventName, model.ConversationId, model);
    }

    private bool AllowSendingMessage()
    {
        var sidecar = _services.GetService<IConversationSideCar>();
        return sidecar == null || !sidecar.IsEnabled;
    }

    private void OnReceiveAssistantMessage(string @event, string conversationId, ChatResponseDto model)
    {
        try
        {
            var settings = _services.GetRequiredService<ChatHubSettings>();
            var chatHub = _services.GetRequiredService<IHubContext<SignalRHub>>();

            if (settings.EventDispatchBy == EventDispatchType.Group)
            {
                chatHub.Clients.Group(conversationId).SendAsync(@event, model).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                var user = _services.GetRequiredService<IUserIdentity>();
                chatHub.Clients.User(user.Id).SendAsync(@event, model).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to receive assistant message in {nameof(ChatHubConversationHook)} (conversation id: {conversationId})");
        }
    }

    private void GenerateSenderAction(string conversationId, ConversationSenderActionModel action)
    {
        try
        {
            var settings = _services.GetRequiredService<ChatHubSettings>();
            var chatHub = _services.GetRequiredService<IHubContext<SignalRHub>>();
            if (settings.EventDispatchBy == EventDispatchType.Group)
            {
                chatHub.Clients.Group(conversationId).SendAsync(ChatEvent.OnSenderActionGenerated, action).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                var user = _services.GetRequiredService<IUserIdentity>();
                chatHub.Clients.User(user.Id).SendAsync(ChatEvent.OnSenderActionGenerated, action).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to generate sender action in {nameof(ChatHubConversationHook)} (conversation id: {conversationId})");
        }
    }
}
