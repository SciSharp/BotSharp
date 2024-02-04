namespace BotSharp.Abstraction.Tasks.Models;

public class AgentTask
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string AgentId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Agent Agent { get; set; }

    public AgentTask()
    {

    }

    public AgentTask(string id, string name, string? description = null)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}
