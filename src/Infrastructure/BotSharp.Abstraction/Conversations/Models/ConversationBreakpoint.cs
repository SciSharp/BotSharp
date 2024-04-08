namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationBreakpoint
{
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("breakpoint")]
    public DateTime Breakpoint { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
