namespace BotSharp.Abstraction.Loggers.Models;

public class ContentLogOutputModel
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = default!;

    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = default!;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = default!;

    [JsonPropertyName("source")]
    public string Source { get; set; } = default!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = default!;

    [JsonPropertyName("created_at")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
