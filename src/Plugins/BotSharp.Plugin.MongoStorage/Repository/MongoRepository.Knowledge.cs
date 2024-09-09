using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public bool ResetKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs)
    {
        var docs = configs?.Select(x => new KnowledgeCollectionConfigDocument
        {
            Id = Guid.NewGuid().ToString(),
            Name = x.Name,
            Type = x.Type,
            TextEmbedding = KnowledgeEmbeddingConfigMongoModel.ToMongoModel(x.TextEmbedding),
            CreateDate = x.CreateDate,
            CreateUserId = x.CreateUserId,
        })?.ToList() ?? new List<KnowledgeCollectionConfigDocument>();

        var filter = Builders<KnowledgeCollectionConfigDocument>.Filter.Empty;
        _dc.KnowledgeCollectionConfigs.DeleteMany(filter);
        _dc.KnowledgeCollectionConfigs.InsertMany(docs);

        return true;
    }

    public VectorCollectionConfig? GetKnowledgeCollectionConfig(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return null;

        var filter = Builders<KnowledgeCollectionConfigDocument>.Filter.Eq(x => x.Name, collectionName);
        var config = _dc.KnowledgeCollectionConfigs.Find(filter).FirstOrDefault();
        if (config == null) return null;

        return new VectorCollectionConfig
        {
            Name = config.Name,
            Type = config.Type,
            TextEmbedding = KnowledgeEmbeddingConfigMongoModel.ToDomainModel(config.TextEmbedding),
            CreateDate = config.CreateDate,
            CreateUserId = config.CreateUserId
        };
    }
}
