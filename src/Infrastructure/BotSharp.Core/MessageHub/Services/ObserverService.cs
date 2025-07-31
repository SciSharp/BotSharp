using BotSharp.Abstraction.MessageHub.Observers;
using BotSharp.Abstraction.MessageHub.Services;
using System.Reactive.Linq;

namespace BotSharp.Core.MessageHub.Services;

public class ObserverService : IObserverService
{
    private readonly IServiceProvider _services;

    public ObserverService(
        IServiceProvider services)
    {
        _services = services;
    }

    public ObserverSubscriptionContainer<T> RegisterObservers<T>(string refId) where T : ObserveDataBase
    {
        var subscriptions = new List<ObserverSubscription<T>>();
        var observers = _services.GetServices<IBotSharpObserver<T>>()
                                 .Where(x => !x.IsActive)
                                 .ToList();

        if (observers.IsNullOrEmpty())
        {
            return new();
        }

        var messageHub = _services.GetRequiredService<MessageHub<T>>();
        foreach (var observer in observers)
        {
            observer.Activate();
            var sub = messageHub.Events.Where(x => x.RefId == refId).Subscribe(observer);
            subscriptions.Add(new()
            {
                Observer = observer,
                Subscription = sub
            });
        }

        return new(subscriptions);
    }
}
