using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentCreationModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Instruction { get; set; }
    public List<string> Functions { get; set; }
    public List<AgentResponse> Responses { get; set; }
    public bool IsPublic { get; set; }

    public Agent ToAgent()
    {
        return new Agent
        {
            Name = Name,
            Description = Description,
            Instruction = Instruction,
            Functions = Functions,
            Responses = Responses,
            IsPublic = IsPublic
        };
    }
}
