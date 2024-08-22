using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentCreationModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; } = AgentType.Task;

    /// <summary>
    /// LLM default system instructions
    /// </summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public List<ChannelInstruction> ChannelInstructions { get; set; } = new();

    /// <summary>
    /// LLM extensible Instructions in addition to the default Instructions
    /// </summary>
    public List<AgentTemplate> Templates { get; set; } = new();

    /// <summary>
    /// LLM callable function definition
    /// </summary>
    public List<FunctionDef> Functions { get; set; } = new();

    /// <summary>
    /// Response template
    /// </summary>
    public List<AgentResponse> Responses { get; set; } = new();
    public List<string> Samples { get; set; } = new();

    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether to allow Router to transfer Request to this Agent
    /// </summary>
    public bool AllowRouting { get; set; }
    public bool Disabled { get; set; }

    /// <summary>
    /// Combine different Agents together to form a Profile.
    /// </summary>
    public List<string> Profiles { get; set; } = new();
    public List<string> Utilities { get; set; } = new();
    public List<RoutingRuleUpdateModel> RoutingRules { get; set; } = new();
    public AgentLlmConfig? LlmConfig { get; set; }

    public Agent ToAgent()
    {
        return new Agent
        {
            Name = Name,
            Description = Description,
            Instruction = Instruction,
            ChannelInstructions = ChannelInstructions,
            Templates = Templates,
            Functions = Functions,
            Responses = Responses,
            Samples = Samples,
            Utilities = Utilities,
            IsPublic = IsPublic,
            Type = Type,
            Disabled = Disabled,
            Profiles = Profiles,
            RoutingRules = RoutingRules?.Select(x => RoutingRuleUpdateModel.ToDomainElement(x))?.ToList() ?? new List<RoutingRule>(),
            LlmConfig = LlmConfig
        };
    }
}
