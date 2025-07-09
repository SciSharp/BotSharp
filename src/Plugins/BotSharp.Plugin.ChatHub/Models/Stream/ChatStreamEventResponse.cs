using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChatHub.Models.Stream;

internal class ChatStreamEventResponse
{
    [JsonPropertyName("event")]
    public string Event { get; set; }

    [JsonPropertyName("body")]
    public ChatStreamEventResponseBody Body { get; set; }
}

internal class ChatStreamEventResponseBody
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; }
}