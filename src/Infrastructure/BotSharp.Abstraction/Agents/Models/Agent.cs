namespace BotSharp.Abstraction.Agents.Models;

public class Agent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
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
    public string Functions { get; set; }

    /// <summary>
    /// Domain knowledges
    /// </summary>
    public string Knowledges { get; set;}
}
