namespace BotSharp.Abstraction.Tasks.Models;

public class AgentTask : AgentTaskMetaData
{
    public string Id { get; set; }
    public string Content { get; set; }

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

public class AgentTaskMetaData
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool Enabled { get; set; }
    public string? DirectAgentId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }
}
