using BotSharp.Abstraction.Conversations.Dtos;
using BotSharp.Abstraction.Observables.Models;
using BotSharp.Abstraction.SideCar;
using BotSharp.Plugin.ChatHub.Hooks;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Observers;

public class ChatHubObserver : IObserver<HubObserveData>
{
    private readonly ILogger _logger;
    private IServiceProvider _services;

    private const string BEFORE_RECEIVE_LLM_STREAM_MESSAGE = "BeforeReceiveLlmStreamMessage";
    private const string ON_RECEIVE_LLM_STREAM_MESSAGE = "OnReceiveLlmStreamMessage";
    private const string AFTER_RECEIVE_LLM_STREAM_MESSAGE = "AfterReceiveLlmStreamMessage";
    private const string GENERATE_SENDER_ACTION = "OnSenderActionGenerated";

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
        if (value.EventName == BEFORE_RECEIVE_LLM_STREAM_MESSAGE)
        {
            var conv = _services.GetRequiredService<IConversationService>();
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

            var action = new ConversationSenderActionModel
            {
                ConversationId = conv.ConversationId,
                SenderAction = SenderActionEnum.TypingOn
            };

            GenerateSenderAction(conv.ConversationId, action).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        else if (value.EventName == AFTER_RECEIVE_LLM_STREAM_MESSAGE)
        {
            if (message.IsStreaming)
            {
                var conv = _services.GetRequiredService<IConversationService>();
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

                var action = new ConversationSenderActionModel
                {
                    ConversationId = conv.ConversationId,
                    SenderAction = SenderActionEnum.TypingOff
                };

                GenerateSenderAction(conv.ConversationId, action).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
        else if (value.EventName == ON_RECEIVE_LLM_STREAM_MESSAGE)
        {
            var conv = _services.GetRequiredService<IConversationService>();
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
        }

        OnReceiveAssistantMessage(value.EventName, model.ConversationId, model).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task OnReceiveAssistantMessage(string @event, string conversationId, ChatResponseDto model)
    {
        try
        {
            var settings = _services.GetRequiredService<ChatHubSettings>();
            var chatHub = _services.GetRequiredService<IHubContext<SignalRHub>>();

            if (settings.EventDispatchBy == EventDispatchType.Group)
            {
                await chatHub.Clients.Group(conversationId).SendAsync(@event, model);
            }
            else
            {
                var user = _services.GetRequiredService<IUserIdentity>();
                await chatHub.Clients.User(user.Id).SendAsync(@event, model);
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
                var user = _services.GetRequiredService<IUserIdentity>();
                await chatHub.Clients.User(user.Id).SendAsync(GENERATE_SENDER_ACTION, action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to generate sender action in {nameof(ChatHubConversationHook)} (conversation id: {conversationId})");
        }
    }
}
