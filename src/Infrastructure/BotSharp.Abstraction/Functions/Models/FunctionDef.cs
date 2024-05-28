namespace BotSharp.Abstraction.Functions.Models;

public class FunctionDef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("visibility_expression")]
    public string? VisibilityExpression { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Impact { get; set; }

    [JsonPropertyName("parameters")]
    public FunctionParametersDef Parameters { get; set; } = new FunctionParametersDef();

    public override string ToString()
    {
        return $"{Name}: {Description}";
    }
}
