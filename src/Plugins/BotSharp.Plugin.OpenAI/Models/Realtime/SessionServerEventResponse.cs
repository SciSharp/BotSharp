namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class SessionServerEventResponse : ServerEventResponse
{
    [JsonPropertyName("session")]
    public RealtimeSessionBody Session { get; set; } = null!;
}
