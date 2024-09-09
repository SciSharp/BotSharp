namespace BotSharp.Plugin.MongoStorage.Collections;

public class KnowledgeCollectionConfigDocument : MongoBase
{
    public string Name { get; set; }
    public string Type { get; set; }
    public KnowledgeEmbeddingConfigMongoModel TextEmbedding { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateUserId { get; set; }
}
