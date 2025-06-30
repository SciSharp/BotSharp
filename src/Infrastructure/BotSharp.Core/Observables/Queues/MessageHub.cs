using System.Reactive.Subjects;

namespace BotSharp.Core.Observables.Queues;

public class MessageHub<T> where T : class
{
    private readonly ILogger<MessageHub<T>> _logger;
    private readonly ISubject<T> _observable = new Subject<T>();
    public IObservable<T> Events => _observable;

    public MessageHub(ILogger<MessageHub<T>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Push an item to the observers.
    /// </summary>
    /// <param name="item"></param>
    public void Push(T item)
    {
        _observable.OnNext(item);
    }

    /// <summary>
    /// Send a complete notification to the observers.
    /// This will stop the observers from receiving data.
    /// </summary>
    public void Complete()
    {
        _observable.OnCompleted();
    }

    /// <summary>
    /// Send an error notification to the observers.
    /// This will stop the observers from receiving data.
    /// </summary>
    /// <param name="error"></param>
    public void Error(Exception error)
    {
        _observable.OnError(error);
    }
}
