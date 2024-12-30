namespace BotSharp.Abstraction.Crontab.Models;

public class CrontabItem : ScheduleTaskArgs
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = null!;

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = null!;

    [JsonPropertyName("execution_result")]
    public string ExecutionResult { get; set; } = null!;

    [JsonPropertyName("execution_count")]
    public int ExecutionCount { get; set; }

    [JsonPropertyName("max_execution_count")]
    public int MaxExecutionCount { get; set; }

    [JsonPropertyName("expire_seconds")]
    public int ExpireSeconds { get; set; } = 60;

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"{Title}: {Description} [AgentId: {AgentId}, UserId: {UserId}]";
    }
}
