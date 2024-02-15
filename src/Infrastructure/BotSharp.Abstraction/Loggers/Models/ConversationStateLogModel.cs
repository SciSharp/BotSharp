namespace BotSharp.Abstraction.Loggers.Models;

public class ConversationStateLogModel
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; }
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }
    [JsonPropertyName("states")]
    public Dictionary<string, string> States { get; set; }
    [JsonPropertyName("created_at")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
