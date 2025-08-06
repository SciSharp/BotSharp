namespace BotSharp.Core.MessageHub.Services;

public class ObserverSubscriptionContainer<T> : IDisposable
{
    private readonly ILogger<ObserverSubscriptionContainer<T>> _logger;

    private List<ObserverSubscription<T>> _subscriptions = [];
    private bool _disposed = false;

    public ObserverSubscriptionContainer(
        ILogger<ObserverSubscriptionContainer<T>> logger)
    {
        _logger = logger;
    }

    public List<ObserverSubscription<T>> GetSubscriptions(IEnumerable<string>? names = null)
    {
        if (!names.IsNullOrEmpty())
        {
            return _subscriptions.Where(x => names.Contains(x.Observer.Name)).ToList();
        }
        return _subscriptions;
    }

    public void Append(List<ObserverSubscription<T>> subscriptions)
    {
        _subscriptions = _subscriptions.Concat(subscriptions).DistinctBy(x => x.Observer.Name).ToList();
    }

    public void Remove(IEnumerable<string>? names = null)
    {
        if (!names.IsNullOrEmpty())
        {
            _subscriptions = _subscriptions.Where(x => !names.Contains(x.Observer.Name)).ToList();
            return;
        }
        _subscriptions.Clear();
    }

    public void Clear()
    {
        _subscriptions.Clear();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
#if DEBUG
            _logger.LogCritical($"Start disposing subscriptions...");
#endif
            // UnSubscribe all observers
            foreach (var sub in _subscriptions)
            {
                sub.UnSubscribe();
            }
            _subscriptions.Clear();
#if DEBUG
            _logger.LogCritical($"End disposing subscriptions...");
#endif
            _disposed = true;
        }
    }
}