using System.Text.Json;

namespace BotSharp.Abstraction.Functions.Models;

/// <summary>
/// This class defines the LLM response output if function call needed
/// </summary>
public class FunctionCallingResponse
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = AgentRole.Assistant;

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("function_name")]
    public string? FunctionName {  get; set; }

    [JsonPropertyName("args")]
    public JsonDocument? Args { get; set; }

    public override string ToString()
    {
        return $"{FunctionName}({JsonSerializer.Serialize(Args)}) => {Content}";
    }
}
