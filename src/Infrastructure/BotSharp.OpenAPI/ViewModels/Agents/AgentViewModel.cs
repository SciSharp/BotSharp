using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Instruction { get; set; }
    public List<AgentTemplate> Templates { get; set; }
    public List<FunctionDef> Functions { get; set; }
    public List<AgentResponse> Responses { get; set; }
    public List<string> Samples { get; set; }
    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("allow_routing")]
    public bool AllowRouting { get; set; }
    public bool Disabled { get; set; }
    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }
    public List<string> Profiles { get; set; }

    [JsonPropertyName("routing_rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RoutingRule> RoutingRules { get; set; }

    [JsonPropertyName("llm_config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentLlmConfig? LlmConfig { get; set; }

    [JsonPropertyName("created_datetime")]
    public DateTime CreatedDateTime { get; set; }

    [JsonPropertyName("updated_datetime")]
    public DateTime UpdatedDateTime { get; set; }

    public static AgentViewModel FromAgent(Agent agent)
    {
        return new AgentViewModel
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Instruction = agent.Instruction,
            Templates = agent.Templates,
            Functions = agent.Functions,
            Responses = agent.Responses,
            Samples = agent.Samples,
            IsPublic= agent.IsPublic,
            Disabled = agent.Disabled,
            IconUrl = agent.IconUrl,
            AllowRouting = agent.AllowRouting,
            Profiles = agent.Profiles,
            RoutingRules = agent.RoutingRules,
            LlmConfig = agent.LlmConfig,
            CreatedDateTime = agent.CreatedDateTime,
            UpdatedDateTime = agent.UpdatedDateTime
        };
    }
}
