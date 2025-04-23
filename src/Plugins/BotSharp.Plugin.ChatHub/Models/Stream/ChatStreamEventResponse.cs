using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChatHub.Models.Stream;

internal class ChatStreamEventResponse
{
    [JsonPropertyName("event")]
    public string Event { get; set; }
}

internal class ChatStreamMediaEventResponse : ChatStreamEventResponse
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; }
}
