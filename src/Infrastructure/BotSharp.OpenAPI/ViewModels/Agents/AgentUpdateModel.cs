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
    /// Channel instructions
    /// </summary>
    [JsonPropertyName("channel_instructions")]
    public List<ChannelInstruction>? ChannelInstructions { get; set; }

    /// <summary>
    /// Templates
    /// </summary>
    public List<AgentTemplate>? Templates { get; set; }

    /// <summary>
    /// Samples
    /// </summary>
    public List<string>? Samples { get; set; }

    [JsonPropertyName("merge_utility")]
    public bool MergeUtility { get; set; }

    /// <summary>
    /// Utilities
    /// </summary>
    public List<AgentUtility>? Utilities { get; set; }

    /// <summary>
    /// knowledge bases
    /// </summary>
    /// 
    [JsonPropertyName("knowledge_bases")]
    public List<AgentKnowledgeBase>? KnowledgeBases { get; set; }

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

    [JsonPropertyName("max_message_count")]
    public int? MaxMessageCount { get; set; }

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
            MergeUtility = MergeUtility,
            MaxMessageCount = MaxMessageCount,
            Type = Type,
            Profiles = Profiles ?? new List<string>(),
            RoutingRules = RoutingRules?.Select(x => RoutingRuleUpdateModel.ToDomainElement(x))?.ToList() ?? new List<RoutingRule>(),
            Instruction = Instruction ?? string.Empty,
            ChannelInstructions = ChannelInstructions ?? new List<ChannelInstruction>(),
            Templates = Templates ?? new List<AgentTemplate>(),
            Functions = Functions ?? new List<FunctionDef>(),
            Responses = Responses ?? new List<AgentResponse>(),
            Utilities = Utilities ?? new List<AgentUtility>(),
            KnowledgeBases = KnowledgeBases ?? [],
            LlmConfig = LlmConfig
        };

        return agent;
    }
}
