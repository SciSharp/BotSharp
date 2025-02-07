using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.Models.Stream;

public class StreamEventMediaResponse : StreamEventResponse
{
    [JsonPropertyName("media")]
    public StreamEventMediaBody Body { get; set; }
}

public class StreamEventMediaBody
{
    [JsonPropertyName("track")]
    public string Track { get; set; }

    [JsonPropertyName("chunk")]
    public string Chunk { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; }

    [JsonPropertyName("payload")]
    public string Payload { get; set; }
}