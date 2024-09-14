using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region Configs
    public bool AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false)
    {
        var filter = Builders<KnowledgeCollectionConfigDocument>.Filter.Empty;
        var docs = configs?.Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new KnowledgeCollectionConfigDocument
            {
                Id = Guid.NewGuid().ToString(),
                Name = x.Name,
                Type = x.Type,
                VectorStore = KnowledgeVectorStoreConfigMongoModel.ToMongoModel(x.VectorStore),
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
                found.VectorStore = doc.VectorStore;
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
            filters.Add(builder.In(x => x.VectorStore.Provider, filter.VectorStroageProviders));
        }

        // Get data
        var configs = _dc.KnowledgeCollectionConfigs.Find(Builders<KnowledgeCollectionConfigDocument>.Filter.And(filters)).ToList();

        return configs.Select(x => new VectorCollectionConfig
        {
            Name = x.Name,
            Type = x.Type,
            VectorStore = KnowledgeVectorStoreConfigMongoModel.ToDomainModel(x.VectorStore),
            TextEmbedding = KnowledgeEmbeddingConfigMongoModel.ToDomainModel(x.TextEmbedding)
        });
    }
    #endregion

    #region Documents
    public bool SaveKnolwedgeBaseFileMeta(KnowledgeDocMetaData metaData)
    {
        if (metaData == null
            || string.IsNullOrWhiteSpace(metaData.Collection)
            || string.IsNullOrWhiteSpace(metaData.VectorStoreProvider)
            || string.IsNullOrWhiteSpace(metaData.FileId))
        {
            return false;
        }

        var doc = new KnowledgeCollectionFileDocument
        {
            Id = Guid.NewGuid().ToString(),
            Collection = metaData.Collection,
            FileId = metaData.FileId,
            FileName = metaData.FileName,
            FileSource = metaData.FileSource,
            ContentType = metaData.ContentType,
            VectorStoreProvider = metaData.VectorStoreProvider,
            VectorDataIds = metaData.VectorDataIds,
            CreateDate = metaData.CreateDate,
            CreateUserId = metaData.CreateUserId
        };

        _dc.KnowledgeCollectionFiles.InsertOne(doc);
        return true;
    }

    public PagedItems<KnowledgeDocMetaData> GetKnowledgeBaseFileMeta(string collectionName, string vectorStoreProvider, KnowledgeFileFilter filter)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider))
        {
            return new PagedItems<KnowledgeDocMetaData>();
        }

        var builder = Builders<KnowledgeCollectionFileDocument>.Filter;
        
        var docFilters = new List<FilterDefinition<KnowledgeCollectionFileDocument>>()
        {
            builder.Eq(x => x.Collection, collectionName),
            builder.Eq(x => x.VectorStoreProvider, vectorStoreProvider)
        };
        
        // Apply filters
        if (filter != null && !filter.FileIds.IsNullOrEmpty())
        {
            docFilters.Add(builder.In(x => x.FileId, filter.FileIds));
        }

        var filterDef = builder.And(docFilters);
        var sortDef = Builders<KnowledgeCollectionFileDocument>.Sort.Descending(x => x.CreateDate);
        var docs = _dc.KnowledgeCollectionFiles.Find(filterDef).Sort(sortDef).Skip(filter.Offset).Limit(filter.Size).ToList();
        var count = _dc.KnowledgeCollectionFiles.CountDocuments(filterDef);

        var files = docs?.Select(x => new KnowledgeDocMetaData
        {
            Collection = x.Collection,
            FileId = x.FileId,
            FileName = x.FileName,
            FileSource = x.FileSource,
            ContentType = x.ContentType,
            VectorStoreProvider = x.VectorStoreProvider,
            VectorDataIds = x.VectorDataIds,
            CreateDate = x.CreateDate,
            CreateUserId = x.CreateUserId
        })?.ToList() ?? new List<KnowledgeDocMetaData>();

        return new PagedItems<KnowledgeDocMetaData>
        {
            Items = files,
            Count = (int)count
        };
    }
    #endregion
}
