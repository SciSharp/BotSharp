namespace BotSharp.Abstraction.MessageHub.Observers;

public interface IBotSharpObserver<T> : IObserver<T>
{
    //string Name { get; }
    bool IsActive { get; }
    void Activate();
    void Deactivate();
}
