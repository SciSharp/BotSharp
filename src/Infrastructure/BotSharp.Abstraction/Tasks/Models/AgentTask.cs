namespace BotSharp.Abstraction.Tasks.Models;

public class AgentTask
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string Content { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string AgentId { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Agent Agent { get; set; } = new();

    /// <summary>
    /// Agent task status
    /// </summary>
    public string Status { get; set; } = TaskStatus.New;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LastExecutionTime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? NextExecutionTime { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; }
}
