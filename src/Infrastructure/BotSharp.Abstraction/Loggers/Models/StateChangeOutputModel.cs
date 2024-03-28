namespace BotSharp.Abstraction.Loggers.Models;

public class StateChangeOutputModel : StateChangeModel
{
    [JsonPropertyName("created_at")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
