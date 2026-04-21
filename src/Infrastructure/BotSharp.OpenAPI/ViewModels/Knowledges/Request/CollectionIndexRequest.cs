using BotSharp.Abstraction.VectorStorage.Options;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class CreateCollectionIndexRequest
{
    [JsonPropertyName("knowledge_type")]
    public string KnowledgeType { get; set; } = null!;

    public IEnumerable<CollectionIndexOptions> Options { get; set; } = [];
}

public class DeleteCollectionIndexRequest
{
    [JsonPropertyName("knowledge_type")]
    public string KnowledgeType { get; set; } = null!;

    public IEnumerable<CollectionIndexOptions> Options { get; set; } = [];
}