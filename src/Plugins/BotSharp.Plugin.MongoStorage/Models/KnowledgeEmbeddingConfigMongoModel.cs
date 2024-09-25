using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class KnowledgeEmbeddingConfigMongoModel
{
    public string Provider { get; set; }
    public string Model { get; set; }
    public int Dimension { get; set; }

    public static KnowledgeEmbeddingConfigMongoModel ToMongoModel(KnowledgeEmbeddingConfig model)
    {
        return new KnowledgeEmbeddingConfigMongoModel
        {
            Provider = model.Provider,
            Model = model.Model,
            Dimension = model.Dimension
        };
    }

    public static KnowledgeEmbeddingConfig ToDomainModel(KnowledgeEmbeddingConfigMongoModel model)
    {
        return new KnowledgeEmbeddingConfig
        {
            Provider = model.Provider,
            Model = model.Model,
            Dimension = model.Dimension
        };
    }
}
