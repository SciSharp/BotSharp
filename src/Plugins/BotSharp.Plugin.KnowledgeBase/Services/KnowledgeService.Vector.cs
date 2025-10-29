using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.VectorStorage.Enums;
using BotSharp.Abstraction.VectorStorage.Filters;
using BotSharp.Abstraction.VectorStorage.Options;

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

    public async Task<bool> CreateVectorCollection(string collectionName, string collectionType, VectorCollectionCreateOptions options)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return false;
            }

            var db = _services.GetRequiredService<IBotSharpRepository>();
            var created = db.AddKnowledgeCollectionConfigs(new List<VectorCollectionConfig>
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
                        Provider = options.Provider,
                        Model = options.Model,
                        Dimension = options.Dimension
                    }
                }
            });

            if (created)
            {
                var vectorDb = GetVectorDb();
                created = await vectorDb.CreateCollection(collectionName, options);
            }

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when creating a vector collection ({collectionName}).");
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
            if (vectorDb == null)
            {
                return [];
            }
            var dbCollections = await vectorDb.GetCollections();
            return configs.Where(x => dbCollections.Contains(x.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when getting vector db collections.");
            return [];
        }
    }

    public async Task<VectorCollectionDetails?> GetVectorCollectionDetails(string collectionName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName)) return null;

            var db = _services.GetRequiredService<IBotSharpRepository>();
            var configs = db.GetKnowledgeCollectionConfigs(new VectorCollectionConfigFilter
            {
                CollectionNames = [collectionName]
            }).ToList();

            var vectorDb = GetVectorDb();
            var details = await vectorDb.GetCollectionDetails(collectionName);
            if (details != null)
            {
                details.BasicInfo = configs.FirstOrDefault();
            }
            return details;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when getting vector db collection details.");
            return null;
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
            _logger.LogError(ex, $"Error when deleting collection ({collectionName}).");
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

            if (!payload.TryGetValue(KnowledgePayloadName.DataSource, out _))
            {
                payload[KnowledgePayloadName.DataSource] = VectorPayloadValue.BuildStringValue(VectorDataSource.Api);
            }

            return await db.Upsert(collectionName, guid, vector, create.Text, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when creating vector collection data.");
            return false;
        }
    }

    public async Task<bool> UpdateVectorCollectionData(string collectionName, VectorUpdateModel update)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName)
                || string.IsNullOrWhiteSpace(update.Text)
                || !Guid.TryParse(update.Id, out var guid))
            {
                return false;
            }

            var db = GetVectorDb();
            var found = await db.GetCollectionData(collectionName, [guid]);
            if (found.IsNullOrEmpty())
            {
                return false;
            }

            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(update.Text);
            var payload = update.Payload ?? new();

            if (!payload.TryGetValue(KnowledgePayloadName.DataSource, out _))
            {

                payload[KnowledgePayloadName.DataSource] = VectorPayloadValue.BuildStringValue(VectorDataSource.Api);
            }

            return await db.Upsert(collectionName, guid, vector, update.Text, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when updating vector collection data.");
            return false;
        }
    }

    public async Task<bool> UpsertVectorCollectionData(string collectionName, VectorUpdateModel update)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName)
                || string.IsNullOrWhiteSpace(update.Text)
                || !Guid.TryParse(update.Id, out var guid))
            {
                return false;
            }

            var db = GetVectorDb();
            var found = await db.GetCollectionData(collectionName, [guid], options: new() { WithVector = true, WithPayload = true });
            if (!found.IsNullOrEmpty())
            {
                if (found.First().Data[KnowledgePayloadName.Text].ToString() == update.Text)
                {
                    // Only update payload
                    return await db.Upsert(collectionName, guid, found.First().Vector, update.Text, update.Payload);
                }
            }

            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(update.Text);
            var payload = update.Payload ?? new();

            if (!payload.TryGetValue(KnowledgePayloadName.DataSource, out _))
            {
                payload[KnowledgePayloadName.DataSource] = VectorPayloadValue.BuildStringValue(VectorDataSource.Api);
            }

            return await db.Upsert(collectionName, guid, vector, update.Text, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when updating vector collection data.");
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
            return await db.DeleteCollectionData(collectionName, [guid]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when deleting vector collection data ({collectionName}-{id}).");
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
            _logger.LogError(ex, $"Error when deleting vector collection data ({collectionName}).");
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
            _logger.LogError(ex, $"Error when getting vector knowledge collection data ({collectionName}).");
            return new StringIdPagedItems<VectorSearchResult>();
        }
    }

    public async Task<IEnumerable<VectorCollectionData>> GetVectorCollectionData(string collectionName, IEnumerable<string> ids, VectorQueryOptions? options = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName) || ids.IsNullOrEmpty())
            {
                return [];
            }

            var pointIds = ids.Select(x => new { Id = x, IsValid = Guid.TryParse(x, out var guid), ParseResult = guid })
                              .Where(x => x.IsValid)
                              .Select(x => x.ParseResult)
                              .ToList();

            var db = GetVectorDb();
            var points = await db.GetCollectionData(collectionName, pointIds, options);
            return points;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when querying vector collection {collectionName} points.");
            return [];
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
            var found = await db.Search(collectionName, vector, options);

            var results = found.Select(x => VectorSearchResult.CopyFrom(x)).ToList();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when searching vector knowledge ({collectionName}).");
            return [];
        }
    }
    #endregion
}
