using BotSharp.Abstraction.Routing.Enums;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingRule
{
    [JsonIgnore]
    public string AgentId { get; set; }

    [JsonIgnore]
    public string AgentName { get; set; }

    public string Type { get; set; } = RuleType.DataValidation;

    public string Field { get; set; }
    public string Description { get; set; }

    /// <summary>
    /// Field type: string, number, object
    /// </summary>
    public string FieldType { get; set; } = "string";

    public bool Required { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RedirectTo { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("redirect_to_agent")]
    public string? RedirectToAgentName { get; set; }

    public override string ToString()
    {
        return $"{Type} {AgentName} {Field}";
    }

    public RoutingRule()
    {
        
    }
}
