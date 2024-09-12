namespace BotSharp.Plugin.MongoStorage.Collections;

public class KnowledgeCollectionConfigDocument : MongoBase
{
    public string Name { get; set; }
    public string Type { get; set; }
    public KnowledgeVectorStoreConfigMongoModel VectorStore { get; set; }
    public KnowledgeEmbeddingConfigMongoModel TextEmbedding { get; set; }
}
