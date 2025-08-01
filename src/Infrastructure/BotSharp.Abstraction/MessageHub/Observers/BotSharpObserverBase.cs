
namespace BotSharp.Abstraction.MessageHub.Observers;

public abstract class BotSharpObserverBase<T> : IBotSharpObserver<T>
{
    protected bool _active = false;

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
