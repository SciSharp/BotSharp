namespace BotSharp.Abstraction.Agents.Models;

public class AgentUtility
{
    public string Category { get; set; }
    public string Name { get; set; }
    public bool Disabled { get; set; }

    [JsonPropertyName("visibility_expression")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VisibilityExpression { get; set; }

    public IEnumerable<UtilityItem> Items { get; set; } = [];

    public AgentUtility()
    {
        
    }

    public override string ToString()
    {
        return $"{Category}-{Name}";
    }
}

public class UtilityItem
{
    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; } = null!;

    [JsonPropertyName("template_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TemplateName { get; set; }

    [JsonPropertyName("visibility_expression")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VisibilityExpression { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}