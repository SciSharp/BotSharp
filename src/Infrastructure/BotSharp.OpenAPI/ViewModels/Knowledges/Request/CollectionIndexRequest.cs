using BotSharp.Abstraction.VectorStorage.Options;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class CreateCollectionIndexRequest : KnowledgeBaseRequestBase
{
    public IEnumerable<CollectionIndexOptions> Options { get; set; } = [];
}

public class DeleteCollectionIndexRequest : KnowledgeBaseRequestBase
{
    public IEnumerable<CollectionIndexOptions> Options { get; set; } = [];
}