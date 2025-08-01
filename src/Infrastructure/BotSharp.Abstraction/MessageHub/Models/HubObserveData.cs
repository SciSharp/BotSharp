namespace BotSharp.Abstraction.MessageHub.Models;

public class HubObserveData<TData> : ObserveDataBase where TData : class, new()
{
    public TData Data { get; set; } = null!;
}
