namespace BotSharp.Abstraction.MessageHub.Models;

public abstract class ObserveDataBase
{
    public IServiceProvider ServiceProvider { get; set; } = null!;
}
