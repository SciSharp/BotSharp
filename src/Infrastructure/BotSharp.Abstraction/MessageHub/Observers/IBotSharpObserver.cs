namespace BotSharp.Abstraction.MessageHub.Observers;

public interface IBotSharpObserver<T> : IObserver<T>
{
    string Name { get; }
    bool Active { get; }

    void SetEventListeners(Dictionary<string, Func<T, Task>> listeners);
    void Activate();
    void Deactivate();
}
