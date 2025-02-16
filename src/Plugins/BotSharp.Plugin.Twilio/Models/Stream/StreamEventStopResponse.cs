using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.Models.Stream;

public class StreamEventStopResponse : StreamEventResponse
{
    [JsonPropertyName("sequenceNumber")]
    public string SequenceNumber { get; set; }

    [JsonPropertyName("streamSid")]
    public string StreamSid { get; set; }

    [JsonPropertyName("stop")]
    public StreamEventStopBody Body { get; set; }
}

public class StreamEventStopBody
{
    [JsonPropertyName("accountSid")]
    public string AccountSid { get; set; }

    [JsonPropertyName("callSid")]
    public string CallSid { get; set; }
}