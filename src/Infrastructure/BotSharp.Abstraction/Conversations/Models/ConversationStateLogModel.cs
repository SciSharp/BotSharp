namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationStateLogModel
{
    [JsonPropertyName("conversation_id")]
    public string ConvsersationId { get; set; }
    [JsonPropertyName("states")]
    public string States { get; set; }
    [JsonPropertyName("created_at")]
    public DateTime CreateTime { get; set; }
}
