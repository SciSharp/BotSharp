namespace BotSharp.Abstraction.Repositories.Records;

public class AgentRecord : RecordBase
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public string Instruction { get; set; }

    public List<string> Functions { get; set; }

    public List<string> Responses { get; set; }

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
            CreatedDateTime = CreatedTime,
            UpdatedDateTime = UpdatedTime
        };
    }

    public AgentRecord SetId(string id)
    {
        Id = id;
        return this;
    }


    public AgentRecord SetInstruction(string instruction)
    {
        Instruction = instruction;
        return this;
    }

    public AgentRecord SetFunctions(List<string> functions)
    {
        Functions = functions;
        return this;
    }

    public AgentRecord SetResponses(List<string> responses)
    {
        Responses = responses;
        return this;
    }
}
