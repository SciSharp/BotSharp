using System.Text.Json;

namespace BotSharp.Abstraction.Routing.Models;

public class RetrievalArgs : RoutingArgs
{
    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("args")]
    public JsonDocument Arguments { get; set; }

    public override string ToString()
    {
        return $"[{AgentName}, {Reason}]: ({JsonSerializer.Serialize(Arguments)}) {Question}";
    }
}
