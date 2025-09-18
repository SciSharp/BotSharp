namespace BotSharp.Abstraction.MessageHub.Models;

public class HubObserveData<TData> : ObserveDataBase where TData : class, new()
{
    /// <summary>
    /// The observed data
    /// </summary>
    public TData Data { get; set; } = null!;

    /// <summary>
    /// Whether to save the observed data To Db
    /// </summary>
    public bool SaveDataToDb { get; set; }
}
