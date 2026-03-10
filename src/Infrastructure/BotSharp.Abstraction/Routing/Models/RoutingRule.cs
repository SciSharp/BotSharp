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
    [JsonPropertyName("field_type")]
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

    /// <summary>
    /// Returns a defensive copy so hook mutations do not affect cached/shared instances.
    /// </summary>
    public RoutingRule Clone() => new RoutingRule
    {
        AgentId = AgentId,
        AgentName = AgentName,
        Type = Type,
        Field = Field,
        Description = Description,
        FieldType = FieldType,
        Required = Required,
        RedirectTo = RedirectTo,
        RedirectToAgentName = RedirectToAgentName
    };
}
