namespace BotSharp.Plugin.MongoStorage.Collections;

public class KnowledgeCollectionConfigDocument : MongoBase
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public KnowledgeVectorStoreConfigMongoModel VectorStore { get; set; } = new();
    public KnowledgeEmbeddingConfigMongoModel TextEmbedding { get; set; } = new();
}
