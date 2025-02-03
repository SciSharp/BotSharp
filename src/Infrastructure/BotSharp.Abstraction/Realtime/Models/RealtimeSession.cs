namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeSession
{
    public string Id { get; set; } = null!;

    public string Object { get; set;} = null!;
    public string Model { get; set; } = null!;
    public string Voice { get; set; } = null!;

    [JsonPropertyName("client_secret")]
    public RealtimeSessionClientSecret Secret { get; set; } = null!;
}

public class RealtimeSessionClientSecret
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;

    [JsonPropertyName("expires_at")]
    public long Expires { get; set; }
}
