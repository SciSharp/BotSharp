using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationFileRequest
{
    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }
}
