using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentCreationModel
{
    public string Name { get; set; }
    public string Description { get; set; }

    public Agent ToAgent()
    {
        return new Agent
        {
            Name = Name,
            Description = Description
        };
    }
}
