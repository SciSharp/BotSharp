using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class UpdateMessageModel
{
    [JsonPropertyName("message")]
    public ChatResponseModel Message { get; set; } = null!;

    [JsonPropertyName("inner_index")]
    public int InnerIndex { get; set; }
}
