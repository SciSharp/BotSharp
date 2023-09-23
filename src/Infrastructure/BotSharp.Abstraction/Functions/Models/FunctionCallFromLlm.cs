using BotSharp.Abstraction.Routing.Models;
using System.Text.Json;

namespace BotSharp.Abstraction.Functions.Models;

public class FunctionCallFromLlm
{
    [JsonPropertyName("function")]
    public string Function { get; set; } = string.Empty;

    [JsonPropertyName("route")]
    public RoutingArgs Route { get; set; } = new RoutingArgs();

    [JsonPropertyName("question")]
    public string? Question { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public JsonDocument Arguments { get; set; } = JsonDocument.Parse("{}");

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Answer))
        {
            return $"[{Function} {Route} {JsonSerializer.Serialize(Arguments)}]: {Question}";
        }
        else
        {
            return $"[{Function} {Route} {JsonSerializer.Serialize(Arguments)}]: {Question} => {Answer}";
        }
    }
}
