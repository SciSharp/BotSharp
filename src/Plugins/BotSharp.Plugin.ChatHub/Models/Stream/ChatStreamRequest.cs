using BotSharp.Abstraction.Models;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChatHub.Models.Stream;

public class ChatStreamRequest
{
    [JsonPropertyName("states")]
    public List<MessageState> States { get; set; } = [];
}
