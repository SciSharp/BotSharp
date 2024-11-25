namespace BotSharp.Abstraction.Infrastructures.Events;

public interface IEventPublisher
{
    /// <summary>
    /// Boardcast message to all subscribers
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task BroadcastAsync(string channel, string message);

    Task PublishAsync(string channel, string message, EventPriority? priority = null);

    Task ReDispatchAsync(string channel, int count = 10, string order = "asc");

    Task ReDispatchPendingAsync(string channel, string group, int count = 10);
}
