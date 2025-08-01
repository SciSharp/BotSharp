
namespace BotSharp.Abstraction.MessageHub.Observers;

public abstract class BotSharpObserverBase<T> : IBotSharpObserver<T>
{
    private bool _active = false;
    protected Dictionary<string, Func<T, Task>> _listeners = [];

    protected BotSharpObserverBase()
    {
        
    }

    public virtual string Name => string.Empty;

    public virtual bool Active => _active;

    public virtual void Activate()
    {
        _active = true;
    }

    public virtual void Deactivate()
    {
        _active = false;
        _listeners = [];
    }

    public virtual void SetEventListeners(Dictionary<string, Func<T, Task>> listeners)
    {
        _listeners = listeners;
    }

    public virtual void OnCompleted()
    {
        
    }

    public virtual void OnError(Exception error)
    {
        
    }

    public virtual void OnNext(T value)
    {
        
    }
}
