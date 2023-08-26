namespace BotSharp.Abstraction.Repositories.Records;

public class AgentRecord : RecordBase
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    [Required]
    public DateTime CreatedTime { get; set; }

    [Required]
    public DateTime UpdatedTime { get; set; }

    public static AgentRecord FromAgent(Agent agent)
    {
        return new AgentRecord
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description
        };
    }

    public Agent ToAgent()
    {
        return new Agent
        {
            Id = Id,
            Name = Name,
            Description = Description,
            CreatedDateTime = CreatedTime,
            UpdatedDateTime = UpdatedTime
        };
    }
}
