namespace BotSharp.Abstraction.Tasks.Models;

public class AgentTask
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; }
    public string Content { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string AgentId { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Agent Agent { get; set; } = new();

    /// <summary>
    /// Agent task status
    /// </summary>
    public string Status { get; set; } = TaskStatus.New;

    public DateTime? LastExecutedDateTime { get; set; }
    public DateTime? NextExecutionDateTime { get; set; }

    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }
}
