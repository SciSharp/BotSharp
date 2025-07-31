using BotSharp.Abstraction.MessageHub.Models;

namespace BotSharp.Abstraction.MessageHub.Services;

public class ObserverSubscriptionContainer<T> : IDisposable
{
    private IList<ObserverSubscription<T>> _subscriptions = [];
    private bool _disposed = false;

    public ObserverSubscriptionContainer()
    {
        
    }

    public ObserverSubscriptionContainer(
        IList<ObserverSubscription<T>> subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
#if DEBUG
                Console.WriteLine($"Start disposing subscriptions...");
#endif
                // Unregister all observers
                foreach (var item in _subscriptions)
                {
                    item.Observer.Deactivate();
                    item.Subscription.Dispose();
                }
                _subscriptions.Clear();
#if DEBUG
                Console.WriteLine($"End disposing subscriptions...");
#endif
            }
            _disposed = true;
        }
    }
}