using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MessageHub.Services;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
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
    /// <param name="callbacks">
    /// Receives tool-call indications, and the reply as it is written when OnStreamText is set
    /// </param>
    public static async Task<AssistantMessage?> SendCallerMessageAsync(
        IServiceProvider sp,
        CallerMessage message,
        AgentTurnCallbacks? callbacks = null)
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

        // Authenticate synchronously and before loading states: this is the fallback identity,
        // which OnStateLoaded may override with the caller's own.
        var request = new ConversationalVoiceRequest
        {
            AgentId = message.AgentId,
            ConversationId = message.ConversationId
        };
        HookEmitter.Emit<ITwilioSessionHook>(sp, hook => hook.OnAuthenticate(request), message.AgentId);

        AssistantMessage? reply = null;

        var inputMsg = new RoleDialogModel(AgentRole.User, message.Content);
        var conv = sp.GetRequiredService<IConversationService>();
        var routing = sp.GetRequiredService<IRoutingService>();
        var observer = sp.GetRequiredService<IObserverService>();

        var streaming = callbacks?.OnStreamText != null;
        var listeners = new Dictionary<string, Func<HubObserveData<RoleDialogModel>, Task>>
        {
            {
                ChatEvent.OnIndicationReceived, async data =>
                {
                    if (callbacks?.OnIndication != null && !string.IsNullOrEmpty(data.Data.Indication))
                    {
                        await callbacks.OnIndication(data.Data.Indication);
                    }
                }
            }
        };
        if (streaming)
        {
            // Listeners run in push order on the model's stream, so the pieces arrive in order
            listeners[ChatEvent.BeforeReceiveLlmStreamMessage] = async _ =>
            {
                if (callbacks!.OnStreamStarted != null)
                {
                    await callbacks.OnStreamStarted();
                }
            };
            listeners[ChatEvent.OnReceiveLlmStreamMessage] = async data =>
            {
                if (!string.IsNullOrEmpty(data.Data.Content))
                {
                    await callbacks!.OnStreamText!(data.Data.Content);
                }
            };
            listeners[ChatEvent.AfterReceiveLlmStreamMessage] = async data =>
            {
                if (data.Data.Role == AgentRole.Function && callbacks!.OnStreamEndedWithToolCall != null)
                {
                    await callbacks.OnStreamEndedWithToolCall();
                }
            };
        }

        using var container = observer.SubscribeObservers<HubObserveData<RoleDialogModel>>(
            message.ConversationId, listeners: listeners);
        await InitConversation(message, inputMsg, conv, routing);

        if (streaming)
        {
            // Routing streams only when both are set (RoutingService, InstructExecutor)
            conv.States.SetState(StateConst.USE_STREAM_MESSAGE, true, source: StateSource.Application);
            if (string.IsNullOrEmpty(conv.States.GetState(StateConst.LANGUAGE)))
            {
                conv.States.SetState(StateConst.LANGUAGE, LanguageType.ENGLISH, source: StateSource.Application);
            }
        }

        if (callbacks != null)
        {
            // Read live while the turn runs, so a tool that turns it on (a confirmation number coming up) covers what
            // the agent says after it in the same turn
            callbacks.KeepUninterrupted = () => conv.States.GetState(ConversationRelayStates.KeepUninterrupted) == "true";
            if (callbacks.LastReplyCutOff)
            {
                conv.States.SetState(ConversationRelayStates.LastReplyCutOff, "true", source: StateSource.Application);
                if (callbacks.LastReplyLine != null && callbacks.LastReplyHeard != null)
                {
                    try
                    {
                        await MarkLastReplyCutOff(sp, message.ConversationId, callbacks.LastReplyLine, callbacks.LastReplyHeard);
                    }
                    catch (Exception ex)
                    {
                        // The turn goes on without the mark: the flow still has LastReplyCutOff
                        sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(TwilioConversationProcessor))
                            .LogWarning(ex, $"Could not mark the reply the caller cut into in conversation {message.ConversationId}.");
                    }
                }
            }
            else
            {
                conv.States.RemoveState(ConversationRelayStates.LastReplyCutOff);
            }
            if (callbacks.OnTurnStarted != null)
            {
                await callbacks.OnTurnStarted();
            }
        }

        // Chat providers stop a streamed response on the conversation's cancellation token, and every response asked
        // for once it is cancelled comes back empty, so a turn the flow cancels ends without saying anything
        var cancellation = streaming ? sp.GetService<IConversationCancellationService>() : null;
        if (cancellation != null)
        {
            cancellation.RegisterConversation(message.ConversationId);
            callbacks!.OnCancel(() => cancellation.CancelStreaming(message.ConversationId));
        }

        try
        {
            await conv.SendMessage(message.AgentId,
                inputMsg,
                replyMessage: await BuildPostbackMessageModel(conv, message),
                async msg =>
                {
                    // Said while the agent worked on the reply: the reply is appended once this returns, so they go in
                    // ahead of it
                    await KeepSaidLines(sp, message.AgentId, message.ConversationId, inputMsg.MessageId,
                        callbacks?.TakeFillers() ?? []);
                    // What the caller is told, which a rewrite below must not change
                    var spoken = msg.Content;
                    RewriteForHistory(msg, callbacks);
                    reply = new AssistantMessage()
                    {
                        ConversationEnd = msg.Instruction?.ConversationEnd ?? false,
                        HumanIntervationNeeded = string.Equals("human_intervention_needed", msg.FunctionName),
                        Content = spoken,
                        MessageId = msg.MessageId,
                        KeepUninterrupted = callbacks?.KeepUninterrupted?.Invoke() ?? false
                    };
                }
            );
        }
        finally
        {
            cancellation?.UnregisterConversation(message.ConversationId);
        }

        return reply;
    }

    /// <summary>
    /// Lets the flow change what the history keeps of the reply. The dialog is appended after this callback runs,
    /// so the message changed here is the one that is stored.
    /// </summary>
    /// <remarks>
    /// An empty reply is offered too: a turn the flow cancelled may have none, and the flow can say why.
    /// </remarks>
    private static void RewriteForHistory(RoleDialogModel msg, AgentTurnCallbacks? callbacks)
    {
        if (callbacks?.RewriteReply == null)
        {
            return;
        }

        var stored = callbacks.RewriteReply(msg.Content ?? string.Empty);
        if (string.IsNullOrEmpty(stored) || stored == msg.Content)
        {
            return;
        }

        // Kept in step only where it is the reply itself; a rich message the model refined is left alone
        if (msg.RichContent?.Message != null && msg.RichContent.Message.Text == msg.Content)
        {
            msg.RichContent.Message.Text = stored;
        }
        msg.Content = stored;
    }

    /// <summary>
    /// Marks the last reply in the history with the part of it the caller missed by cutting in, as a reply cut off while
    /// it streamed is marked: the history took the line whole, as a line a tool gave is said only once its turn is over
    /// </summary>
    /// <remarks>
    /// Why: the agent took "yes, that's" as an answer to "…is the number you're calling from, ending in 7, 8, 2, 4, the
    /// best one to reach you?" when the caller had heard it only up to "ending in 7, 8, 2", and did not ask again "Is this
    /// one of those?", cut off after "You already have three open requests for your AC." (dev call recording).
    /// </remarks>
    private static async Task MarkLastReplyCutOff(IServiceProvider sp, string conversationId, string line, string heard)
    {
        var storage = sp.GetRequiredService<IConversationStorage>();
        var dialogs = await storage.GetDialogs(conversationId);
        var last = dialogs.LastOrDefault(d => d.Role == AgentRole.Assistant && !d.ExcludeFromContext
            && !string.IsNullOrWhiteSpace(d.Content));
        var marked = last == null ? null : ConversationRelayCutIn.MarkStored(last.Content, line, heard);
        if (marked == null)
        {
            return;
        }

        // The storage finds the dialog by its message id and role, then its place among those
        var index = dialogs.Where(d => d.MessageId == last!.MessageId && d.Role == last.Role).ToList().IndexOf(last!);
        var richContent = last!.RichContent;
        if (richContent?.Message != null && richContent.Message.Text == last.Content)
        {
            richContent.Message.Text = marked;
        }
        var options = sp.GetRequiredService<BotSharpOptions>();
        var element = new DialogElement
        {
            MetaData = new DialogMetaData { Role = last.Role, MessageId = last.MessageId },
            Content = marked,
            RichContent = richContent == null ? null : JsonSerializer.Serialize(richContent, options.JsonSerializerOptions)
        };
        await sp.GetRequiredService<IConversationService>().UpdateConversationMessage(conversationId,
            new UpdateMessageRequest { Message = element, InnerIndex = index });
    }

    /// <summary>
    /// Keeps lines the caller heard in the history, out of the agent's context: the fillers said while the agent worked
    /// on a reply, kept just ahead of it (a reply to one, "Sure.", makes sense only next to it), and what a
    /// ConversationRelay session says on its own (an apology, an idle prompt, a line said again)
    /// </summary>
    /// <param name="messageId">The caller message the lines go with; null gives each line a message of its own</param>
    public static async Task KeepSaidLines(IServiceProvider sp, string agentId, string conversationId, string? messageId,
        IReadOnlyCollection<(string Text, DateTime SaidUtc)> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var dialogs = lines.Select(line => new RoleDialogModel(AgentRole.Assistant, line.Text)
        {
            CurrentAgentId = agentId,
            MessageId = messageId ?? Guid.NewGuid().ToString(),
            CreatedAt = line.SaidUtc,
            ExcludeFromContext = true
        }).ToList();
        try
        {
            await sp.GetRequiredService<IConversationStorage>().Append(conversationId, dialogs);
        }
        catch (Exception ex)
        {
            // The call goes on, and a reply is still stored; only the record of these lines is lost
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(TwilioConversationProcessor))
                .LogWarning(ex, $"Could not keep what was said in conversation {conversationId}.");
        }
    }

    private static async Task<PostbackMessageModel?> BuildPostbackMessageModel(
        IConversationService conv, CallerMessage message)
    {
        // A line kept only as a record of what the caller heard (KeepSaidLines) may have been said after the reply that
        // asked for the postback
        var lastMessage = (await conv.GetDialogHistory()).LastOrDefault(x => !x.ExcludeFromContext);
        if (lastMessage == null) return null;
        if (string.IsNullOrEmpty(lastMessage.PostbackFunctionName)) return null;
        return new PostbackMessageModel
        {
            FunctionName = lastMessage.PostbackFunctionName,
            ParentId = lastMessage.MessageId,
            Payload = message.Digits
        };
    }

    private static async Task InitConversation(
        CallerMessage message, RoleDialogModel inputMsg, IConversationService conv, IRoutingService routing)
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
