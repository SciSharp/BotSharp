using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class KnowledgeEmbeddingConfigMongoModel
{
    public string Provider { get; set; } = default!;
    public string Model { get; set; } = default!;
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
