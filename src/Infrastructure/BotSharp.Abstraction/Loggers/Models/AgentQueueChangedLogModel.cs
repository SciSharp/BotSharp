namespace BotSharp.Abstraction.Loggers.Models;

public class AgentQueueChangedLogModel
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; }

    [JsonPropertyName("log")]
    public string Log { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreateTime { get; set; }
}
