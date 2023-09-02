using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Instruction { get; set; }
    public List<string> Functions { get; set; }
    public List<string> Responses { get; set; }
    public DateTime UpdatedDateTime { get; set; }

    public static AgentViewModel FromAgent(Agent agent)
    {
        return new AgentViewModel
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Instruction = agent.Instruction,
            Functions = agent.Functions,
            Responses = agent.Responses,
            UpdatedDateTime = agent.UpdatedDateTime
        };
    }
}
