using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionConfigsRequest
{
    [JsonPropertyName("collections")]
    public List<VectorCollectionConfig> Collections { get; set; } = new();
}
