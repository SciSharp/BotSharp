using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChatHub.Models.Stream;

internal class ChatStreamEventResponse
{
    [JsonPropertyName("event")]
    public string Event { get; set; }
}

internal class ChatStreamMediaEventResponse : ChatStreamEventResponse
{
    [JsonPropertyName("body")]
    public MediaEventResponseBody Body { get; set; }
}

internal class MediaEventResponseBody
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; }
}