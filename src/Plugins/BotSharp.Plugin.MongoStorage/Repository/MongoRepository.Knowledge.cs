using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public bool AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false)
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

        if (reset)
        {
            var filter = Builders<KnowledgeCollectionConfigDocument>.Filter.Empty;
            _dc.KnowledgeCollectionConfigs.DeleteMany(filter);
        }

        _dc.KnowledgeCollectionConfigs.InsertMany(docs);
        return true;
    }

    public bool DeleteKnowledgeCollectionConfig(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return false;

        var filter = Builders<KnowledgeCollectionConfigDocument>.Filter.Eq(x => x.Name, collectionName);
        var deleted = _dc.KnowledgeCollectionConfigs.DeleteMany(filter);
        return deleted.DeletedCount > 0;
    }

    public IEnumerable<VectorCollectionConfig> GetKnowledgeCollectionConfigs(VectorCollectionConfigFilter filter)
    {
        if (filter == null)
        {
            return Enumerable.Empty<VectorCollectionConfig>();
        }

        var builder = Builders<KnowledgeCollectionConfigDocument>.Filter;
        var filters = new List<FilterDefinition<KnowledgeCollectionConfigDocument>> { builder.Empty };

        var configs = _dc.KnowledgeCollectionConfigs.Find(Builders<KnowledgeCollectionConfigDocument>.Filter.And(filters)).ToList();


        return configs.Select(x => new VectorCollectionConfig
        {
            Name = x.Name,
            Type = x.Type,
            TextEmbedding = KnowledgeEmbeddingConfigMongoModel.ToDomainModel(x.TextEmbedding),
            CreateDate = x.CreateDate,
            CreateUserId= x.CreateUserId
        });
    }
}
