using BotSharp.Abstraction.VectorStorage.Filters;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public abstract partial class VectorOrchestratorBase
{
    #region Collection
    public virtual async Task<bool> ExistCollection(string collectionName, KnowledgeCollectionOptions options)
    {
        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var exist = await vectorDb.DoesCollectionExist(collectionName);
        if (exist)
        {
            return true;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var configs = await db.GetKnowledgeCollectionConfigs(new VectorCollectionConfigFilter
        {
            CollectionNames = [collectionName],
            VectorStorageProviders = [vectorDb.Provider]
        });

        return !configs.IsNullOrEmpty();
    }

    public virtual async Task<bool> CreateCollection(string collectionName, CollectionCreateOptions options)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return false;
        }

        var vectorDb = GetVectorDb(options.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var created = await db.AddKnowledgeCollectionConfigs(new List<VectorCollectionConfig>
        {
            new VectorCollectionConfig
            {
                Name = collectionName,
                Type = KnowledgeType,
                VectorStore = new VectorStoreConfig
                {
                    Provider = vectorDb.Provider
                },
                TextEmbedding = new KnowledgeEmbeddingConfig
                {
                    Provider = options.LlmProvider,
                    Model = options.LlmModel,
                    Dimension = options.EmbeddingDimension
                }
            }
        });

        if (!created)
        {
            return false;
        }

        created = await vectorDb.CreateCollection(collectionName, options: new()
        {
            Provider = options.LlmProvider,
            Model = options.LlmProvider,
            Dimension = options.EmbeddingDimension
        });

        return created;
    }

    public virtual async Task<IEnumerable<KnowledgeCollectionConfig>> GetCollections(KnowledgeCollectionOptions options)
    {
        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return [];
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var configs = await db.GetKnowledgeCollectionConfigs(new VectorCollectionConfigFilter
        {
            CollectionTypes = !string.IsNullOrEmpty(KnowledgeType) ? [KnowledgeType] : null,
            VectorStorageProviders = [vectorDb.Provider]
        });
        
        var dbCollections = await vectorDb.GetCollections();
        return configs.Where(x => dbCollections.Contains(x.Name)).Select(x => new KnowledgeCollectionConfig
        {
            Name = x.Name,
            Type = x.Type,
            VectorStore = x.VectorStore,
            TextEmbedding = x.TextEmbedding
        });
    }

    public virtual async Task<bool> DeleteCollection(string collectionName, KnowledgeCollectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return false;
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var deleted = await vectorDb.DeleteCollection(collectionName);

        if (deleted)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var fileStorage = _services.GetRequiredService<IFileStorageService>();

            await db.DeleteKnowledgeCollectionConfig(collectionName);
            fileStorage.DeleteKnowledgeFile(collectionName, vectorDb.Provider);
            await db.DeleteKnolwedgeBaseFileMeta(collectionName, vectorDb.Provider);
        }

        return deleted;
    }
    #endregion
}
