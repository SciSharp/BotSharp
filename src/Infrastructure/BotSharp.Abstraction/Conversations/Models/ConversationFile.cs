namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationFile
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; }

    [JsonPropertyName("thumbnail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
}
