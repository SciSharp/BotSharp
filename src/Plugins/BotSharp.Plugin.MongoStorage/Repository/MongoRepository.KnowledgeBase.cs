using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public bool AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false)
    {
        var filter = Builders<KnowledgeCollectionConfigDocument>.Filter.Empty;
        var docs = configs?.Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new KnowledgeCollectionConfigDocument
            {
                Id = Guid.NewGuid().ToString(),
                Name = x.Name,
                Type = x.Type,
                VectorStorage = KnowledgeVectorStorageConfigMongoModel.ToMongoModel(x.VectorStorage),
                TextEmbedding = KnowledgeEmbeddingConfigMongoModel.ToMongoModel(x.TextEmbedding)
            })?.ToList() ?? new List<KnowledgeCollectionConfigDocument>();

        if (reset)
        {
            _dc.KnowledgeCollectionConfigs.DeleteMany(filter);
            _dc.KnowledgeCollectionConfigs.InsertMany(docs);
            return true;
        }

        // Update if collection already exists, otherwise insert.
        var insertDocs = new List<KnowledgeCollectionConfigDocument>();
        var updateDocs = new List<KnowledgeCollectionConfigDocument>();

        var names = docs.Select(x => x.Name).ToList();
        filter = Builders<KnowledgeCollectionConfigDocument>.Filter.In(x => x.Name, names);
        var savedConfigs = _dc.KnowledgeCollectionConfigs.Find(filter).ToList();
        
        foreach (var doc in docs)
        {
            var found = savedConfigs.FirstOrDefault(x => x.Name == doc.Name);
            if (found != null)
            {
                found.Type = doc.Type;
                found.VectorStorage = doc.VectorStorage;
                found.TextEmbedding = doc.TextEmbedding;
                updateDocs.Add(found);
            }
            else
            {
                insertDocs.Add(doc);
            }
        }

        if (!insertDocs.IsNullOrEmpty())
        {
            _dc.KnowledgeCollectionConfigs.InsertMany(docs);
        }

        if (!updateDocs.IsNullOrEmpty())
        {
            foreach (var doc in updateDocs)
            {
                filter = Builders<KnowledgeCollectionConfigDocument>.Filter.Eq(x => x.Id, doc.Id);
                _dc.KnowledgeCollectionConfigs.ReplaceOne(filter, doc);
            }
        }
        
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

        // Apply filters
        if (!filter.CollectionNames.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.Name, filter.CollectionNames));
        }

        if (!filter.CollectionTypes.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.Type, filter.CollectionTypes));
        }

        if (!filter.VectorStroageProviders.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.VectorStorage.Provider, filter.VectorStroageProviders));
        }

        // Get data
        var configs = _dc.KnowledgeCollectionConfigs.Find(Builders<KnowledgeCollectionConfigDocument>.Filter.And(filters)).ToList();

        return configs.Select(x => new VectorCollectionConfig
        {
            Name = x.Name,
            Type = x.Type,
            VectorStorage = KnowledgeVectorStorageConfigMongoModel.ToDomainModel(x.VectorStorage),
            TextEmbedding = KnowledgeEmbeddingConfigMongoModel.ToDomainModel(x.TextEmbedding)
        });
    }
}
