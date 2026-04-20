using BotSharp.Abstraction.VectorStorage.Options;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class CreateVectorCollectionIndexRequest
{
    public IEnumerable<CollectionIndexOptions> Options { get; set; } = [];
}

public class DeleteVectorCollectionIndexRequest
{
    public IEnumerable<CollectionIndexOptions> Options { get; set; } = [];
}