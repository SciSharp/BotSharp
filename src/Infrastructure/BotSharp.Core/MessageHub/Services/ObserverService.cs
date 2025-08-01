using BotSharp.Abstraction.MessageHub.Observers;
using BotSharp.Abstraction.MessageHub.Services;
using System.Reactive.Linq;

namespace BotSharp.Core.MessageHub.Services;

public class ObserverService : IObserverService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ObserverService> _logger;

    public ObserverService(
        IServiceProvider services,
        ILogger<ObserverService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public IDisposable SubscribeObservers<T>(string refId, IEnumerable<string>? names = null) where T : ObserveDataBase
    {
        var container = _services.GetRequiredService<ObserverSubscriptionContainer<T>>();
        var observers = _services.GetServices<IBotSharpObserver<T>>()
                                 .Where(x => !x.Active)
                                 .ToList();

        if (!names.IsNullOrEmpty())
        {
            observers = observers.Where(x => names.Contains(x.Name)).ToList();
        }

        if (observers.IsNullOrEmpty())
        {
            return container;
        }

#if DEBUG
        _logger.LogCritical($"Subscribe observers: {string.Join(",", observers.Select(x => x.Name))}");
#endif

        var subscriptions = new List<ObserverSubscription<T>>();
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

        container.Append(subscriptions);
        return container;
    }

    public void UnSubscribeObservers<T>(IEnumerable<string>? names = null) where T : ObserveDataBase
    {
        var container = _services.GetRequiredService<ObserverSubscriptionContainer<T>>();
        var subscriptions = container.GetSubscriptions(names);

#if DEBUG
        _logger.LogCritical($"UnSubscribe observers: {string.Join(",", subscriptions.Select(x => x.Observer.Name))}");
#endif

        foreach (var sub in subscriptions)
        {
            sub.UnSubscribe();
        }
    }
}
