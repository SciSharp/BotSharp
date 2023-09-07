namespace BotSharp.Abstraction.Agents.Models;

public class Agent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }

    /// <summary>
    /// Instruction
    /// </summary>
    public string Instruction { get; set; }

    /// <summary>
    /// Samples
    /// </summary>
    public string Samples { get; set; }

    /// <summary>
    /// Functions
    /// </summary>
    public List<string> Functions { get; set; }

    /// <summary>
    /// Responses
    /// </summary>
    public List<string> Responses { get; set; }

    /// <summary>
    /// Domain knowledges
    /// </summary>
    public string Knowledges { get; set; }

    public bool IsPublic { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }

    public override string ToString()
        => $"{Name} {Id}";


    public static Agent Clone(Agent agent)
    {
        return new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Instruction = agent.Instruction,
            Functions = agent.Functions,
            Responses = agent.Responses,
            Samples = agent.Samples,
            Knowledges = agent.Knowledges,
            IsPublic = agent.IsPublic,
            CreatedDateTime = agent.CreatedDateTime,
            UpdatedDateTime = agent.UpdatedDateTime,
        };
    }

    public Agent SetInstruction(string instruction)
    {
        Instruction = instruction;
        return this;
    }

    public Agent SetFunctions(List<string> functions)
    {
        Functions = functions ?? new List<string>();
        return this;
    }

    public Agent SetResponses(List<string> responses)
    {
        Responses = responses ?? new List<string>(); ;
        return this;
    }

    public Agent SetId(string id)
    {
        Id = id;
        return this;
    }

    public Agent SetName(string name)
    {
        Name = name;
        return this;
    }

    public Agent SetDescription(string description)
    {
        Description = description;
        return this;
    }

    public Agent SetIsPublic(bool isPublic)
    {
        IsPublic = isPublic;
        return this;
    }
}
