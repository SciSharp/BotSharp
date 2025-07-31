namespace BotSharp.Abstraction.MessageHub.Models;

public class ObserveDataBase
{
    public IServiceProvider ServiceProvider { get; set; } = null!;
    public string RefId { get; set; } = null!;
}
