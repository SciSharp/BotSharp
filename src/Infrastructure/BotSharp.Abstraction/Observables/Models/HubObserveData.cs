namespace BotSharp.Abstraction.Observables.Models;

public class HubObserveData : ObserveDataBase
{
    public string EventName { get; set; } = null!;
    public RoleDialogModel Data { get; set; } = null!;
}
