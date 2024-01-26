using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentUpdateModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = AgentType.Task;
    /// <summary>
    /// Instruction
    /// </summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>
    /// Templates
    /// </summary>
    public List<AgentTemplate>? Templates { get; set; }

    /// <summary>
    /// Samples
    /// </summary>
    public List<string>? Samples { get; set; }

    /// <summary>
    /// Functions
    /// </summary>
    public List<FunctionDef>? Functions { get; set; }

    /// <summary>
    /// Routes
    /// </summary>
    public List<AgentResponse>? Responses { get; set; }
    [JsonPropertyName("is_public")]

    public bool IsPublic { get; set; }
    [JsonPropertyName("allow_routing")]

    public bool AllowRouting { get; set; }

    public bool Disabled { get; set; }

    /// <summary>
    /// Profile by channel
    /// </summary>
    public List<string>? Profiles { get; set; }
    [JsonPropertyName("routing_rules")]

    public List<RoutingRuleUpdateModel>? RoutingRules { get; set; }

    [JsonPropertyName("llm_config")]
    public AgentLlmConfig? LlmConfig { get; set; }

    public Agent ToAgent()
    {
        var agent = new Agent()
        {
            Name = Name ?? string.Empty,
            Description = Description ?? string.Empty,
            IsPublic = IsPublic,
            Disabled = Disabled,
            Type = Type,
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
