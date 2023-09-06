using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Routing.Models;

public class RetrievalArgs : RoutingArgs
{
    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; }

    [JsonPropertyName("response")]
    public string Response { get; set; }

    [JsonPropertyName("args")]
    public JsonDocument Arguments { get; set; }
}
