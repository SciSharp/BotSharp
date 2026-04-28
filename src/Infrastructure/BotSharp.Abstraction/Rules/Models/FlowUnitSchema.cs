using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Rules.Models;

/// <summary>
/// Describes the input or output contract of a rule flow unit (action or condition).
/// Follows a JSON Schema-like structure with "properties" and "required" fields.
/// </summary>
public class FlowUnitSchema
{
    /// <summary>
    /// Property definitions keyed by parameter name.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, FlowUnitSchemaProperty> Properties { get; set; } = [];

    /// <summary>
    /// List of required property names.
    /// </summary>
    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = [];

    public FlowUnitSchema() { }

    public FlowUnitSchema(
        Dictionary<string, FlowUnitSchemaProperty> properties,
        List<string>? required = null)
    {
        Properties = properties;
        Required = required ?? [];
    }
}

/// <summary>
/// Describes a single property in a FlowUnitSchema.
/// </summary>
public class FlowUnitSchemaProperty
{
    /// <summary>
    /// JSON type: "string", "number", "boolean", "object", "array"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    /// <summary>
    /// A brief explanation of the property's purpose.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    public FlowUnitSchemaProperty() { }

    public FlowUnitSchemaProperty(string type, string? description = null)
    {
        Type = type;
        Description = description;
    }
}
