using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.Abstraction.Agents.Models;

public class Agent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Agent Type
    /// </summary>
    public string Type { get; set; } = AgentType.Task;
    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }

    /// <summary>
    /// Default LLM settings
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentLlmConfig LlmConfig { get; set; } = new();

    /// <summary>
    /// Instruction
    /// </summary>
    [JsonIgnore]
    public string? Instruction { get; set; }

    /// <summary>
    /// Channel instructions
    /// </summary>
    [JsonIgnore]
    public List<ChannelInstruction> ChannelInstructions { get; set; } = new();

    /// <summary>
    /// Templates
    /// </summary>
    [JsonIgnore]
    public List<AgentTemplate> Templates { get; set; } = new();

    /// <summary>
    /// Agent tasks
    /// </summary>
    [JsonIgnore]
    public List<AgentTask> Tasks { get; set; } = new();

    /// <summary>
    /// Samples
    /// </summary>
    [JsonIgnore]
    public List<string> Samples { get; set; } = new();

    /// <summary>
    /// Functions
    /// </summary>
    [JsonIgnore]
    public List<FunctionDef> Functions { get; set; } = new();

    /// <summary>
    /// Responses
    /// </summary>
    [JsonIgnore]
    public List<AgentResponse>? Responses { get; set; }

    /// <summary>
    /// Domain knowledges
    /// </summary>
    [JsonIgnore]
    public string? Knowledges { get; set; }

    public bool IsPublic { get; set; }

    [JsonIgnore]
    public PluginDef Plugin {  get; set; }

    [JsonIgnore]
    public bool Installed => Plugin.Enabled;

    /// <summary>
    /// Default is True, user will enable this by installing appropriate plugin.
    /// </summary>
    public bool Disabled { get; set; } = true;
    public string? IconUrl { get; set; }

    /// <summary>
    /// Profile by channel
    /// </summary>
    public List<string> Profiles { get; set; } = new();

    /// <summary>
    /// Merge utilities from entry agent
    /// </summary>
    public bool MergeUtility { get; set; }

    /// <summary>
    /// Agent utilities
    /// </summary>
    public List<AgentUtility> Utilities { get; set; } = new();

    /// <summary>
    /// Inherit from agent
    /// </summary>
    public string? InheritAgentId { get; set; }

    public List<RoutingRule> RoutingRules { get; set; } = new();

    /// <summary>
    /// For rendering deferral
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object> TemplateDict { get; set; } = new();

    public override string ToString()
        => $"{Name} {Id}";


    public static Agent Clone(Agent agent)
    {
        return new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Type = agent.Type,
            Instruction = agent.Instruction,
            ChannelInstructions = agent.ChannelInstructions,
            Functions = agent.Functions,
            Responses = agent.Responses,
            Samples = agent.Samples,
            Utilities = agent.Utilities,
            Knowledges = agent.Knowledges,
            IsPublic = agent.IsPublic,
            Disabled = agent.Disabled,
            Profiles = agent.Profiles,
            RoutingRules = agent.RoutingRules,
            LlmConfig = agent.LlmConfig,
            CreatedDateTime = agent.CreatedDateTime,
            UpdatedDateTime = agent.UpdatedDateTime,
        };
    }

    public Agent SetInstruction(string instruction)
    {
        Instruction = instruction;
        return this;
    }

    public Agent SetChannelInstructions(List<ChannelInstruction> instructions)
    {
        ChannelInstructions = instructions ?? new List<ChannelInstruction>();
        return this;
    }

    public Agent SetTemplates(List<AgentTemplate> templates)
    {
        Templates = templates ?? new List<AgentTemplate>();
        return this;
    }

    public Agent SetTasks(List<AgentTask> tasks)
    {
        Tasks = tasks ?? new List<AgentTask>();
        return this;
    }

    public Agent SetFunctions(List<FunctionDef> functions)
    {
        Functions = functions ?? new List<FunctionDef>();
        return this;
    }

    public Agent SetSamples(List<string> samples)
    {
        Samples = samples ?? new List<string>();
        return this;
    }

    public Agent SetUtilities(List<AgentUtility> utilities)
    {
        Utilities = utilities ?? new List<AgentUtility>();
        return this;
    }

    public Agent SetResponses(List<AgentResponse> responses)
    {
        Responses = responses ?? new List<AgentResponse>(); ;
        return this;
    }

    public Agent SetId(string id)
    {
        Id = id;
        return this;
    }

    public Agent SetName(string name)
    {
        Name = name;
        return this;
    }

    public Agent SetDescription(string description)
    {
        Description = description;
        return this;
    }

    public Agent SetIsPublic(bool isPublic)
    {
        IsPublic = isPublic;
        return this;
    }

    public Agent SetDisabled(bool disabled)
    {
        Disabled = disabled;
        return this;
    }

    public Agent SetMergeUtility(bool merge)
    {
        MergeUtility = merge;
        return this;
    }

    public Agent SetAgentType(string type)
    {
        Type = type;
        return this;
    }

    public Agent SetProfiles(List<string> profiles)
    {
        Profiles = profiles ?? new List<string>();
        return this;
    }

    public Agent SetRoutingRules(List<RoutingRule> rules)
    {
        RoutingRules = rules ?? new List<RoutingRule>();
        return this;
    }

    public Agent SetLlmConfig(AgentLlmConfig? llmConfig)
    {
        LlmConfig = llmConfig;
        return this;
    }
}
