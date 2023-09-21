using System.Text.Json;

namespace BotSharp.Abstraction.Routing.Models;

public class RetrievalArgs : RoutingArgs
{
    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; }

    [JsonPropertyName("args")]
    public JsonDocument Arguments { get; set; } = JsonDocument.Parse("{}");

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Answer))
        {
            return $"[{AgentName}]: ({JsonSerializer.Serialize(Arguments)}) {Question}";
        }
        else
        {
            return $"[{AgentName}]: ({JsonSerializer.Serialize(Arguments)}) {Question} => {Answer}";
        }
    }
}
