using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.VectorStorage.Enums;
using System;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    #region Collection
    public async Task<bool> ExistVectorCollection(string collectionName)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var vectorDb = GetVectorDb();

        var exist = await vectorDb.DoesCollectionExist(collectionName);
        if (exist) return true;

        var configs = db.GetKnowledgeCollectionConfigs(new VectorCollectionConfigFilter
        {
            CollectionNames = [collectionName],
            VectorStroageProviders = [_settings.VectorDb.Provider]
        });

        return !configs.IsNullOrEmpty();
    }

    public async Task<bool> CreateVectorCollection(string collectionName, string collectionType, int dimension, string provider, string model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return false;
            }

            var vectorDb = GetVectorDb();
            var created = await vectorDb.CreateCollection(collectionName, dimension);
            if (created)
            {
                var db = _services.GetRequiredService<IBotSharpRepository>();
                var userId = await GetUserId();

                db.AddKnowledgeCollectionConfigs(new List<VectorCollectionConfig>
                {
                    new VectorCollectionConfig
                    {
                        Name = collectionName,
                        Type = collectionType,
                        VectorStore = new VectorStoreConfig
                        {
                            Provider = _settings.VectorDb.Provider
                        },
                        TextEmbedding = new KnowledgeEmbeddingConfig
                        {
                            Provider = provider,
                            Model = model,
                            Dimension = dimension
                        }
                    }
                });
            }

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when creating a vector collection ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public async Task<IEnumerable<VectorCollectionConfig>> GetVectorCollections(string? type = null)
    {
        try
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var configs = db.GetKnowledgeCollectionConfigs(new VectorCollectionConfigFilter
            {
                CollectionTypes = !string.IsNullOrEmpty(type) ? [type] : null,
                VectorStroageProviders = [_settings.VectorDb.Provider]
            }).ToList();

            var vectorDb = GetVectorDb();
            var dbCollections = await vectorDb.GetCollections();
            return configs.Where(x => dbCollections.Contains(x.Name));
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting vector db collections. {ex.Message}\r\n{ex.InnerException}");
            return Enumerable.Empty<VectorCollectionConfig>();
        }
    }

    public async Task<bool> DeleteVectorCollection(string collectionName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return false;
            }

            var vectorDb = GetVectorDb();
            var deleted = await vectorDb.DeleteCollection(collectionName);

            if (deleted)
            {
                var db = _services.GetRequiredService<IBotSharpRepository>();
                var fileStorage = _services.GetRequiredService<IFileStorageService>();
                var vectorStoreProvider = _settings.VectorDb.Provider;

                db.DeleteKnowledgeCollectionConfig(collectionName);
                fileStorage.DeleteKnowledgeFile(collectionName, vectorStoreProvider);
                db.DeleteKnolwedgeBaseFileMeta(collectionName, vectorStoreProvider);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when deleting collection ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }
    #endregion

    #region Collection data
    public async Task<bool> CreateVectorCollectionData(string collectionName, VectorCreateModel create)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(create.Text))
            {
                return false;
            }

            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(create.Text);

            var db = GetVectorDb();
            var guid = Guid.NewGuid();
            var payload = create.Payload ?? new();
            payload[KnowledgePayloadName.DataSource] = !string.IsNullOrWhiteSpace(create.DataSource) ? create.DataSource : VectorDataSource.Api;

            return await db.Upsert(collectionName, guid, vector, create.Text, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when creating vector collection data. {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public async Task<bool> UpdateVectorCollectionData(string collectionName, VectorUpdateModel update)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(update.Text) || !Guid.TryParse(update.Id, out var guid))
            {
                return false;
            }

            var db = GetVectorDb();
            var found = await db.GetCollectionData(collectionName, new List<Guid> { guid });
            if (found.IsNullOrEmpty())
            {
                return false;
            }

            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(update.Text);
            var payload = update.Payload ?? new();
            payload[KnowledgePayloadName.DataSource] = !string.IsNullOrWhiteSpace(update.DataSource) ? update.DataSource : VectorDataSource.Api;

            return await db.Upsert(collectionName, guid, vector, update.Text, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when updating vector collection data. {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public async Task<bool> UpsertVectorCollectionData(string collectionName, VectorUpdateModel update)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(update.Text) || !Guid.TryParse(update.Id, out var guid))
            {
                return false;
            }

            var db = GetVectorDb();
            var found = await db.GetCollectionData(collectionName, new List<Guid> { guid }, 
                withVector: true, 
                withPayload: true);
            if (!found.IsNullOrEmpty())
            {
                if (found.First().Data["text"].ToString() == update.Text)
                {
                    // Only update payload
                    return await db.Upsert(collectionName, guid, found.First().Vector, update.Text, update.Payload);
                }
            }

            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(update.Text);
            var payload = update.Payload ?? new();
            payload[KnowledgePayloadName.DataSource] = !string.IsNullOrWhiteSpace(update.DataSource) ? update.DataSource : VectorDataSource.Api;

            return await db.Upsert(collectionName, guid, vector, update.Text, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when updating vector collection data. {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public async Task<bool> DeleteVectorCollectionData(string collectionName, string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
            {
                return false;
            }

            var db = GetVectorDb();
            return await db.DeleteCollectionData(collectionName, new List<Guid> { guid });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when deleting vector collection data ({collectionName}-{id}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }


    public async Task<bool> DeleteVectorCollectionAllData(string collectionName)
    {
        try
        {
            var db = GetVectorDb();
            return await db.DeleteCollectionAllData(collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when deleting vector collection data ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public async Task<StringIdPagedItems<VectorSearchResult>> GetPagedVectorCollectionData(string collectionName, VectorFilter filter)
    {
        try
        {
            var db = GetVectorDb();
            var pagedResult = await db.GetPagedCollectionData(collectionName, filter);
            return new StringIdPagedItems<VectorSearchResult>
            {
                Count = pagedResult.Count,
                Items = pagedResult.Items.Select(x => VectorSearchResult.CopyFrom(x)),
                NextId = pagedResult.NextId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting vector knowledge collection data ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return new StringIdPagedItems<VectorSearchResult>();
        }
    }

    public async Task<IEnumerable<VectorSearchResult>> SearchVectorKnowledge(string query, string collectionName, VectorSearchOptions options)
    {
        try
        {
            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(query);

            // Vector search
            var db = GetVectorDb();
            var found = await db.Search(collectionName, vector, options.Fields, limit: options.Limit ?? 5, confidence: options.Confidence ?? 0.5f, withVector: options.WithVector);

            var results = found.Select(x => VectorSearchResult.CopyFrom(x)).ToList();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when searching vector knowledge ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return Enumerable.Empty<VectorSearchResult>();
        }
    }
    #endregion
}
