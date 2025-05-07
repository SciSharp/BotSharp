using BotSharp.Abstraction.Conversations.Dtos;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class UpdateMessageModel
{
    [JsonPropertyName("message")]
    public ChatResponseDto Message { get; set; } = null!;

    [JsonPropertyName("inner_index")]
    public int InnerIndex { get; set; }
}
