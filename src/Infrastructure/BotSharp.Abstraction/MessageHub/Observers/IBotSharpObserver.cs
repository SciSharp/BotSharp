namespace BotSharp.Abstraction.MessageHub.Observers;

public interface IBotSharpObserver<T> : IObserver<T>
{
    string Name { get; }
    bool Active { get; }
    void Activate();
    void Deactivate();
}
