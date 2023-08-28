namespace BotSharp.Abstraction.Repositories.Records;

public class AgentRecord : RecordBase
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public string Instruction { get; set; }

    public string Functions { get; set; }

    public List<string> Routes { get; set; }

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
            Description = agent.Description,
            Instruction = agent.Instruction,
            Functions = agent.Functions,
            Routes = agent.Routes,
        };
    }

    public Agent ToAgent()
    {
        return new Agent
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Instruction = Instruction,
            Functions = Functions,
            Routes = Routes,
            CreatedDateTime = CreatedTime,
            UpdatedDateTime = UpdatedTime
        };
    }
}
