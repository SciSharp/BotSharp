using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.ViewModels;

public class AgentViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime UpdatedDateTime { get; set; }

    public static AgentViewModel FromAgent(Agent agent)
    {
        return new AgentViewModel
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            UpdatedDateTime = agent.UpdatedDateTime
        };
    }
}
