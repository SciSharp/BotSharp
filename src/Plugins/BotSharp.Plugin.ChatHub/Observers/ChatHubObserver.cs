using BotSharp.Abstraction.Conversations.Dtos;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MessageHub.Observers;
using BotSharp.Abstraction.SideCar;
using System.Runtime.CompilerServices;

namespace BotSharp.Plugin.ChatHub.Observers;

public class ChatHubObserver : BotSharpObserverBase<HubObserveData<RoleDialogModel>>
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;

    public ChatHubObserver(
        IServiceProvider services,
        ILogger<ChatHubObserver> logger) : base()
    {
        _services = services;
        _logger = logger;
    }

    public override string Name => nameof(ChatHubObserver);

    public override void OnCompleted()
    {
        _logger.LogWarning($"{nameof(ChatHubObserver)} receives complete notification.");
    }

    public override void OnError(Exception error)
    {
        _logger.LogError(error, $"{nameof(ChatHubObserver)} receives error notification: {error.Message}");
    }

    public override void OnNext(HubObserveData<RoleDialogModel> value)
    {
        var message = value.Data;
        var model = new ChatResponseDto();
        var action = new ConversationSenderActionModel();
        var conv = _services.GetRequiredService<IConversationService>();

        switch (value.EventName)
        {
            case ChatEvent.BeforeReceiveLlmStreamMessage:
                if (!AllowSendingMessage()) return;

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

                SendEvent(ChatEvent.OnSenderActionGenerated, conv.ConversationId, action);
                break;
            case ChatEvent.OnReceiveLlmStreamMessage:
                if (!AllowSendingMessage()) return;

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
                if (!AllowSendingMessage()) return;

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

#if DEBUG
                _logger.LogCritical($"Receiving {value.EventName} ({value.Data.Indication}) in {nameof(ChatHubObserver)} - {conv.ConversationId}");
#endif
                break;
        }

        SendEvent(value.EventName, model.ConversationId, model);
    }

    private bool AllowSendingMessage()
    {
        var sidecar = _services.GetService<IConversationSideCar>();
        return sidecar == null || !sidecar.IsEnabled;
    }

    #region Private methods
    private void SendEvent<T>(string @event, string conversationId, T data, [CallerMemberName] string callerName = "")
    {
        var user = _services.GetRequiredService<IUserIdentity>();
        EventEmitter.SendChatEvent(_services, _logger, @event, conversationId, user?.Id, data, nameof(ChatHubObserver), callerName)
                     .ConfigureAwait(false).GetAwaiter().GetResult();
    }
    #endregion
}
