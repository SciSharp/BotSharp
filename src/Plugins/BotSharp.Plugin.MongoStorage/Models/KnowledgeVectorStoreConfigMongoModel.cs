using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class KnowledgeVectorStoreConfigMongoModel
{
    public string Provider { get; set; }

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
