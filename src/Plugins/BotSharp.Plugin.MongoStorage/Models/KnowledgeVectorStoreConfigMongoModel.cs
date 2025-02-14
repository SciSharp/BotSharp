using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class KnowledgeVectorStoreConfigMongoModel
{
    public string Provider { get; set; } = default!;

    public static KnowledgeVectorStoreConfigMongoModel ToMongoModel(VectorStoreConfig model)
    {
        return new KnowledgeVectorStoreConfigMongoModel
        {
            Provider = model.Provider
        };
    }

    public static VectorStoreConfig ToDomainModel(KnowledgeVectorStoreConfigMongoModel model)
    {
        return new VectorStoreConfig
        {
            Provider = model.Provider
        };
    }
}
