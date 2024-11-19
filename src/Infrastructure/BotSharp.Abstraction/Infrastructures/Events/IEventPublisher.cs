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

    Task PublishAsync(string channel, string message);

    Task ReDispatchAsync(string channel, int count = 10, string order = "asc");
}
