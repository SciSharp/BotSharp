using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Graph.Models;

public class GraphQueryRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; }

    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Method { get; set; }
}
