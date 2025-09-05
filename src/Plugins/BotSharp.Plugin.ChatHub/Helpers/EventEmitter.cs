using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;

namespace BotSharp.Plugin.ChatHub.Helpers;

public class EventEmitter
{
    public static async Task SendChatEvent<T>(
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

            switch (settings.EventDispatchBy)
            {
                case EventDispatchType.Group when !string.IsNullOrEmpty(conversationId):
                    await chatHub.Clients.Group(conversationId).SendAsync(@event, data);
                    break;
                case EventDispatchType.User when !string.IsNullOrEmpty(userId):
                    await chatHub.Clients.User(userId).SendAsync(@event, data);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.Log(logLevel, ex, $"Failed to send event '{@event}' in ({callerClass}-{callerMethod}) (conversation id: {conversationId})");
        }
    }
}
