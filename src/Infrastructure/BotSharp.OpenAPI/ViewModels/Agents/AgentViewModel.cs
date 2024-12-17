using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Routing.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; } = AgentType.Task;
    public string Instruction { get; set; }

    [JsonPropertyName("channel_instructions")]
    public List<ChannelInstruction> ChannelInstructions { get; set; }
    public List<AgentTemplate> Templates { get; set; }
    public List<FunctionDef> Functions { get; set; }
    public List<AgentResponse> Responses { get; set; }
    public List<string> Samples { get; set; }

    [JsonPropertyName("merge_utility")]
    public bool MergeUtility { get; set; }
    public List<AgentUtility> Utilities { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("is_host")]
    public bool IsHost { get; set; }

    public bool Disabled { get; set; }

    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    public List<string> Profiles { get; set; } = new();

    [JsonPropertyName("routing_rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RoutingRule> RoutingRules { get; set; }

    [JsonPropertyName("llm_config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentLlmConfig? LlmConfig { get; set; }

    [JsonPropertyName("max_message_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxMessageCount { get; set; }

    public PluginDef Plugin { get; set; }

    public IEnumerable<string>? Actions { get; set; }

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
            Type = agent.Type,
            Instruction = agent.Instruction,
            ChannelInstructions = agent.ChannelInstructions,
            Templates = agent.Templates,
            Functions = agent.Functions,
            Responses = agent.Responses,
            Samples = agent.Samples,
            Utilities = agent.Utilities,
            IsPublic= agent.IsPublic,
            Disabled = agent.Disabled,
            MergeUtility = agent.MergeUtility,
            IconUrl = agent.IconUrl,
            MaxMessageCount = agent.MaxMessageCount,
            Profiles = agent.Profiles ?? new List<string>(),
            RoutingRules = agent.RoutingRules,
            LlmConfig = agent.LlmConfig,
            Plugin = agent.Plugin,
            CreatedDateTime = agent.CreatedDateTime,
            UpdatedDateTime = agent.UpdatedDateTime
        };
    }
}
