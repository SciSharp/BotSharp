using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;

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
    public List<FunctionDef>? Functions { get; set; }

    /// <summary>
    /// Routes
    /// </summary>
    public List<AgentResponse>? Responses { get; set; }

    public bool IsPublic { get; set; }

    public bool AllowRouting { get; set; }

    public bool Disabled { get; set; }

    /// <summary>
    /// Profile by channel
    /// </summary>
    public List<string>? Profiles { get; set; }

    public List<RoutingRuleUpdateModel>? RoutingRules { get; set; }

    public AgentLlmConfig? LlmConfig { get; set; }

    public Agent ToAgent()
    {
        var agent = new Agent()
        {
            Name = Name ?? string.Empty,
            Description = Description ?? string.Empty,
            IsPublic = IsPublic,
            Disabled = Disabled,
            AllowRouting = AllowRouting,
            Profiles = Profiles ?? new List<string>(),
            RoutingRules = RoutingRules?
                            .Select(x => RoutingRuleUpdateModel.ToDomainElement(x))?
                            .ToList() ?? new List<RoutingRule>(),
            Instruction = Instruction ?? string.Empty,
            Templates = Templates ?? new List<AgentTemplate>(),
            Functions = Functions ?? new List<FunctionDef>(),
            Responses = Responses ?? new List<AgentResponse>(),
            LlmConfig = LlmConfig
        };

        return agent;
    }
}
