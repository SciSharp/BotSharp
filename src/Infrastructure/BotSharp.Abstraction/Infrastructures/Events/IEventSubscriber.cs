namespace BotSharp.Abstraction.Infrastructures.Events;

public interface IEventSubscriber
{
    Task SubscribeAsync(string channel, Func<string, string, Task> received);

    Task SubscribeAsync(string channel, string group, bool priorityEnabled, Func<string, string, Task> received);
}
