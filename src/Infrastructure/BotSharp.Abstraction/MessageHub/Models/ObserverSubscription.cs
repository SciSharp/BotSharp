using BotSharp.Abstraction.MessageHub.Observers;

namespace BotSharp.Abstraction.MessageHub.Models;

public class ObserverSubscription<T>
{
    public IBotSharpObserver<T> Observer { get; set; }
    public IDisposable Subscription { get; set; }

    public ObserverSubscription()
    {
        
    }

    public ObserverSubscription(
        IBotSharpObserver<T> observer,
        IDisposable subscription)
    {
        Observer = observer;
        Subscription = subscription;
    }

    public void UnSubscribe()
    {
        Observer.Deactivate();
        Subscription.Dispose();
    }
}
