namespace BotSharp.Abstraction.Crontab.Models;

public class CrontabItem : ScheduleTaskArgs
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = null!;

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = null!;

    [JsonPropertyName("execution_result")]
    public string ExecutionResult { get; set; } = null!;

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"{Topic}: {Description} [AgentId: {AgentId}, UserId: {UserId}]";
    }
}
