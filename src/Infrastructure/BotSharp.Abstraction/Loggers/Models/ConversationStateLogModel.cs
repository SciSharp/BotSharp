namespace BotSharp.Abstraction.Loggers.Models;

public class ConversationStateLogModel
{
    [JsonPropertyName("conversation_id")] public string ConversationId { get; set; } = default!;

    [JsonPropertyName("agent_id")] public string AgentId { get; set; } = default!;

    [JsonPropertyName("message_id")] public string MessageId { get; set; } = default!;

    [JsonPropertyName("states")]
    public Dictionary<string, string> States { get; set; } = [];

    [JsonPropertyName("created_at")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
