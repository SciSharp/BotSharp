using BotSharp.Abstraction.Models;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChatHub.Models.Stream;

public class ChatStreamRequest
{
    [JsonPropertyName("states")]
    public List<MessageState> States { get; set; } = [];

    [JsonPropertyName("realtime_options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RealtimeOptions? Options { get; set; }
}
