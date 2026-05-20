using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionConfigsRequest
{
    public List<VectorCollectionConfig> Collections { get; set; } = new();
}
