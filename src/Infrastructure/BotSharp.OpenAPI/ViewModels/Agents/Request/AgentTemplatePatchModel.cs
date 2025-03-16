using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentTemplatePatchModel
{
    public List<AgentTemplate>? Templates { get; set; }

    public AgentTemplatePatchModel()
    {
        
    }

    public Agent ToAgent()
    {
        var agent = new Agent()
        {
            Templates = Templates ?? new List<AgentTemplate>(),
        };

        return agent;
    }
}
