using BotSharp.Abstraction.MessageHub.Models;

namespace BotSharp.Abstraction.MessageHub.Services;

public interface IObserverService
{
    IDisposable SubscribeObservers<T>(string refId, IEnumerable<string>? names = null) where T : ObserveDataBase;

    void UnSubscribeObservers<T>(IEnumerable<string>? names = null) where T : ObserveDataBase;
}
