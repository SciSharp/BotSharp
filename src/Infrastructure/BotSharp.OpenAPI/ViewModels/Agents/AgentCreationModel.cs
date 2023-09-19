using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentCreationModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Instruction { get; set; }
    public List<AgentTemplate> Templates { get; set; }
    public List<string> Functions { get; set; }
    public List<AgentResponse> Responses { get; set; }
    public bool IsPublic { get; set; }
    public bool AllowRouting { get; set; }
    public bool Disabled { get; set; }
    public List<string> Profiles { get; set; }
    public List<RoutingRuleUpdateModel> RoutingRules { get; set; }

    public Agent ToAgent()
    {
        return new Agent
        {
            Name = Name,
            Description = Description,
            Instruction = Instruction,
            Templates = Templates,
            Functions = Functions,
            Responses = Responses,
            IsPublic = IsPublic,
            AllowRouting = AllowRouting,
            Disabled = Disabled,
            Profiles = Profiles,
            RoutingRules = RoutingRules?
                            .Select(x => RoutingRuleUpdateModel.ToDomainElement(x))?
                            .ToList() ?? new List<RoutingRule>()
        };
    }
}
