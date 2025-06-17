namespace BotSharp.Abstraction.Observables.Models;

public abstract class ObserveDataBase
{
    public IServiceProvider ServiceProvider { get; set; } = null!;
}
