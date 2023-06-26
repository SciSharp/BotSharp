using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.ViewModels;

public class AgentUpdateModel
{
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>
    /// Instruction
    /// </summary>
    public string Instruction { get; set; }

    /// <summary>
    /// Samples
    /// </summary>
    public string Samples { get; set; }

    public Agent ToAgent()
    {
        return new Agent
        {
            Name = Name,
            Description = Description,
            Instruction = Instruction,
            Samples = Samples
        };
    }
}
