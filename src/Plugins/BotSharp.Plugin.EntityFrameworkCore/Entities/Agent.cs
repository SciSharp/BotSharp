using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class Agent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string? InheritAgentId { get; set; }
    public string? IconUrl { get; set; }
    public string Instruction { get; set; }

    [Column(TypeName = "json")]
    public List<ChannelInstruction> ChannelInstructions { get; set; }

    [Column(TypeName = "json")]
    public List<AgentTemplate> Templates { get; set; }

    [Column(TypeName = "json")]
    public List<FunctionDef> Functions { get; set; }

    [Column(TypeName = "json")]
    public List<AgentResponse> Responses { get; set; }

    [Column(TypeName = "json")]
    public List<string> Samples { get; set; }

    [Column(TypeName = "json")]
    public List<string> Utilities { get; set; }
    public bool IsPublic { get; set; }
    public bool Disabled { get; set; }

    [Column(TypeName = "json")]
    public List<string> Profiles { get; set; }

    [Column(TypeName = "json")]
    public List<RoutingRule> RoutingRules { get; set; }

    [Column(TypeName = "json")]
    public AgentLlmConfig? LlmConfig { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
