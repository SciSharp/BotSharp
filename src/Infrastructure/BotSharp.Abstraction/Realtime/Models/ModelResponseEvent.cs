namespace BotSharp.Abstraction.Realtime.Models;

public class ModelResponseEvent
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;
}
