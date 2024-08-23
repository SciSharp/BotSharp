using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeCreateRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("payload")]
    public Dictionary<string, string>? Payload { get; set; }
}
