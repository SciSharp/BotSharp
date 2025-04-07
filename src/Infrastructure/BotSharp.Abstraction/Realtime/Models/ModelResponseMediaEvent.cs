namespace BotSharp.Abstraction.Realtime.Models;

public class ModelResponseMediaEvent : ModelResponseEvent
{
    [JsonPropertyName("media")]
    public string Media { get; set; } = null!;
}
