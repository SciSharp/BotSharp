using System.Text.Json;

namespace BotSharp.Abstraction.Functions.Models;

public class FunctionParametersDef
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    /// <summary>
    /// ParameterPropertyDef
    /// {
    ///     "field_name": {}
    /// }
    /// </summary>
    [JsonPropertyName("properties")]
    public JsonDocument Properties { get; set; } = JsonSerializer.Deserialize<JsonDocument>("{}");

    [JsonPropertyName("required")]
    public List<string> Required {  get; set; } = new List<string>();

    public FunctionParametersDef()
    {
        
    }
}
