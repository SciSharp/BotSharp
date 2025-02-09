namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class ServerEventResponse
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;
}
