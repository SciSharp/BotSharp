using BotSharp.Abstraction.Observables.Models;
using System.Reactive.Subjects;

namespace BotSharp.Core.Observables.Queues;

public class MessageHub
{
    private readonly ILogger<MessageHub> _logger;
    private readonly ISubject<HubObserveData> _observable = new Subject<HubObserveData>();
    public IObservable<HubObserveData> Events => _observable;

    public MessageHub(ILogger<MessageHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Push an item to the observers.
    /// </summary>
    /// <param name="item"></param>
    public void Push(HubObserveData item)
    {
        _logger.LogInformation($"Pushing item to observers: {item.Data.Content}");
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
