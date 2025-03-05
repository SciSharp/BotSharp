using BotSharp.Plugin.Twilio.Models.Stream;
using System.Text.Json.Serialization;

public class StreamEventDtmfResponse : StreamEventResponse
{
    [JsonPropertyName("dtmf")]
    public StreamEventDtmfBody Body { get; set; }
}

public class StreamEventDtmfBody
{
    [JsonPropertyName("track")]
    public string Track { get; set; }

    [JsonPropertyName("digit")]
    public string Digit { get; set; }
}