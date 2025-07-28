using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;

namespace BotSharp.Plugin.ChatHub.Helpers;

public class ChatHubHelper
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

            if (settings.EventDispatchBy == EventDispatchType.Group)
            {
                await chatHub.Clients.Group(conversationId).SendAsync(@event, data);
            }
            else
            {
                await chatHub.Clients.User(userId).SendAsync(@event, data);
            }
        }
        catch (Exception ex)
        {
            logger.Log(logLevel, ex, $"Failed to send event '{@event}' in ({callerClass}-{callerMethod}) (conversation id: {conversationId})");
        }
    }
}
