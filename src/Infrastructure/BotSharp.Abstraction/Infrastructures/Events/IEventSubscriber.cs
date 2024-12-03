using System.Threading;

namespace BotSharp.Abstraction.Infrastructures.Events;

public interface IEventSubscriber
{
    Task SubscribeAsync(string channel, Func<string, string, Task> received);

    Task SubscribeAsync(string channel, string group, int? port, bool priorityEnabled, 
        Func<string, string, Task> received,
        CancellationToken? stoppingToken = null);
}
