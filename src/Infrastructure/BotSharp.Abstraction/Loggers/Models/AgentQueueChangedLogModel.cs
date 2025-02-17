namespace BotSharp.Abstraction.Loggers.Models;

public class AgentQueueChangedLogModel
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = default!;

    [JsonPropertyName("log")]
    public string Log { get; set; } = default!;

    [JsonPropertyName("created_at")]
    public DateTime CreatedTime { get; set; }
}
