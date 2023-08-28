using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentUpdateModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Instruction
    /// </summary>
    public string? Instruction { get; set; }

    /// <summary>
    /// Samples
    /// </summary>
    public string? Samples { get; set; }

    /// <summary>
    /// Functions
    /// </summary>
    public string? Functions { get; set; }

    /// <summary>
    /// Routes
    /// </summary>
    public List<string>? Routes { get; set; }

    public Agent ToAgent()
    {
        var agent = new Agent
        {
            Name = Name
        };

        if (Description != null)
            agent.Description = Description;

        if (Instruction != null)
            agent.Instruction = Instruction;

        if (Samples != null)
            agent.Samples = Samples;

        if (Functions != null)
            agent.Functions = Functions;

        if (!Routes.IsEmpty())
            agent.Routes = Routes;

        return agent;
    }
}
