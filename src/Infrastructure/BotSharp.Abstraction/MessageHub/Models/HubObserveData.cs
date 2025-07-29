namespace BotSharp.Abstraction.MessageHub.Models;

public class HubObserveData<TData> : ObserveDataBase where TData : class, new()
{
    public string EventName { get; set; } = null!;
    public TData Data { get; set; } = null!;
}
