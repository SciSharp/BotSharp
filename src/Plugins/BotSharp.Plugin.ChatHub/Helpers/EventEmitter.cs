using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BotSharp.Plugin.ChatHub.Helpers;

internal class EventEmitter
{
    /// <summary>
    /// Address one chat event and hand it to <see cref="ChatEventDispatcher"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns as soon as the event is QUEUED, not when the client has it. The awaits at the call
    /// sites are therefore free, and the one caller that cannot await — <c>ChatHubObserver</c>,
    /// which is an <c>IObserver</c> and runs on whatever thread pushed to the MessageHub — no
    /// longer stops that thread on a socket. See the dispatcher for what a stalled client used to
    /// cost.
    /// </para>
    /// <para>
    /// Everything that needs the request's services is resolved HERE, on the caller's thread, and
    /// captured: the proxy is taken from the singleton hub context, so a delivery running after the
    /// scope has been disposed still has what it needs.
    /// </para>
    /// </remarks>
    internal static Task SendChatEvent<T>(
        IServiceProvider services,
        ILogger logger,
        string @event,
        string conversationId,
        string userId,
        T data,
        string callerClass = "",
        [CallerMemberName] string callerMethod = "",
        LogLevel logLevel = LogLevel.Warning)
    {
        try
        {
            var settings = services.GetRequiredService<ChatHubSettings>();
            var chatHub = services.GetRequiredService<IHubContext<SignalRHub>>();
            var dispatcher = services.GetRequiredService<ChatEventDispatcher>();

            IClientProxy? clients = null;
            string? target = null;

            switch (settings.EventDispatchBy)
            {
                case EventDispatchType.Group when !string.IsNullOrEmpty(conversationId):
                    clients = chatHub.Clients.Group(conversationId);
                    // Prefixed so the two dispatch modes cannot share one delivery chain.
                    target = $"{EventDispatchType.Group}:{conversationId}";
                    break;
                case EventDispatchType.User when !string.IsNullOrEmpty(userId):
                    clients = chatHub.Clients.User(userId);
                    target = $"{EventDispatchType.User}:{userId}";
                    break;
            }

            if (clients == null || target == null)
            {
                return Task.CompletedTask;
            }

            dispatcher.Enqueue(
                target,
                ct => clients.SendAsync(@event, data, ct),
                logger,
                $"{@event} ({callerClass}-{callerMethod}) (conversation id: {conversationId})");
        }
        catch (Exception ex)
        {
            logger.Log(logLevel, ex, $"Failed to send event '{@event}' in ({callerClass}-{callerMethod}) (conversation id: {conversationId})");
        }

        return Task.CompletedTask;
    }
}
