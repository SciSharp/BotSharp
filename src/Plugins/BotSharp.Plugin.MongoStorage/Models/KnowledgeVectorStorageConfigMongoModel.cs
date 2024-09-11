using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class KnowledgeVectorStorageConfigMongoModel
{
    public string Provider { get; set; }

    public static KnowledgeVectorStorageConfigMongoModel ToMongoModel(VectorStorageConfig model)
    {
        return new KnowledgeVectorStorageConfigMongoModel
        {
            Provider = model.Provider
        };
    }

    public static VectorStorageConfig ToDomainModel(KnowledgeVectorStorageConfigMongoModel model)
    {
        return new VectorStorageConfig
        {
            Provider = model.Provider
        };
    }
}
