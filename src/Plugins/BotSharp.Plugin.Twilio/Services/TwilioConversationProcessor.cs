using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MessageHub.Services;
using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.Twilio.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// Sends what the caller said to the agent and returns the agent's reply.
/// Shared by the webhook flow (TwilioMessageQueueService) and the ConversationRelay WebSocket flow.
/// </summary>
public static class TwilioConversationProcessor
{
    /// <param name="sp">A service scope created for this one caller message</param>
    /// <param name="onIndication">Receives tool-call indications while the agent is working</param>
    public static async Task<AssistantMessage?> SendCallerMessageAsync(
        IServiceProvider sp,
        CallerMessage message,
        Func<string, Task>? onIndication = null)
    {
        // Clean static HttpContext
        var httpContext = sp.GetRequiredService<IHttpContextAccessor>();
        httpContext.HttpContext = new DefaultHttpContext();
        httpContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        foreach (var header in message.RequestHeaders ?? [])
        {
            httpContext.HttpContext.Request.Headers[header.Key] = header.Value;
        }
        httpContext.HttpContext.Request.Headers["X-Twilio-BotSharp"] = "LOST";

        AssistantMessage? reply = null;

        var inputMsg = new RoleDialogModel(AgentRole.User, message.Content);
        var conv = sp.GetRequiredService<IConversationService>();
        var routing = sp.GetRequiredService<IRoutingService>();
        var observer = sp.GetRequiredService<IObserverService>();

        using var container = observer.SubscribeObservers<HubObserveData<RoleDialogModel>>(message.ConversationId, listeners: new()
        {
            {
                ChatEvent.OnIndicationReceived, async data =>
                {
                    if (onIndication != null && !string.IsNullOrEmpty(data.Data.Indication))
                    {
                        await onIndication(data.Data.Indication);
                    }
                }
            }
        });
        await InitConversation(message, inputMsg, conv, routing);

        // Need to consider Inbound and Outbound call
        var conversation = await conv.GetConversation(message.ConversationId);

        await conv.SendMessage(message.AgentId,
            inputMsg,
            replyMessage: await BuildPostbackMessageModel(conv, message),
            async msg =>
            {
                reply = new AssistantMessage()
                {
                    ConversationEnd = msg.Instruction?.ConversationEnd ?? false,
                    HumanIntervationNeeded = string.Equals("human_intervention_needed", msg.FunctionName),
                    Content = msg.Content,
                    MessageId = msg.MessageId
                };
            }
        );

        return reply;
    }

    private static async Task<PostbackMessageModel?> BuildPostbackMessageModel(IConversationService conv, CallerMessage message)
    {
        var messages = await conv.GetDialogHistory(1);
        if (!messages.Any()) return null;
        var lastMessage = messages[0];
        if (string.IsNullOrEmpty(lastMessage.PostbackFunctionName)) return null;
        return new PostbackMessageModel
        {
            FunctionName = lastMessage.PostbackFunctionName,
            ParentId = lastMessage.MessageId,
            Payload = message.Digits
        };
    }

    private static async Task InitConversation(CallerMessage message, RoleDialogModel inputMsg, IConversationService conv, IRoutingService routing)
    {
        routing.Context.SetMessageId(message.ConversationId, inputMsg.MessageId);
        var states = new List<MessageState>
        {
            new(StateConst.CHANNEL, ConversationChannel.Phone),
            new("channel_id", message.From),
            new("calling_phone", message.From)
        };
        states.AddRange(message.States.Select(kvp => new MessageState(kvp.Key, kvp.Value)));
        await conv.SetConversationId(message.ConversationId, states);
    }
}
