namespace BotSharp.Abstraction.Loggers.Models;

public class ConversationContentLogModel
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; }
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
