using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentUpdateModel
{
    public string? Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Instruction
    /// </summary>
    public string? Instruction { get; set; }

    /// <summary>
    /// Templates
    /// </summary>
    public List<AgentTemplate>? Templates { get; set; }

    /// <summary>
    /// Samples
    /// </summary>
    public string? Samples { get; set; }

    /// <summary>
    /// Functions
    /// </summary>
    public List<string>? Functions { get; set; }

    /// <summary>
    /// Routes
    /// </summary>
    public List<AgentResponse>? Responses { get; set; }

    public Agent ToAgent()
    {
        var agent = new Agent()
        {
            Name = Name ?? string.Empty,
            Description = Description ?? string.Empty,
            Instruction = Instruction ?? string.Empty,
            Templates = Templates ?? new List<AgentTemplate>(),
            Functions = Functions ?? new List<string>(),
            Responses = Responses ?? new List<AgentResponse>()
        };

        return agent;
    }
}
