using BotSharp.Abstraction.MessageHub.Models;

namespace BotSharp.Abstraction.MessageHub.Services;

public interface IObserverService
{
    ObserverSubscriptionContainer<T> RegisterObservers<T>(string refId) where T : ObserveDataBase;
}
