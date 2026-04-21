using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCreateRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("payload")]
    public Dictionary<string, VectorPayloadValue>? Payload { get; set; }
}
