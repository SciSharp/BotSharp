using BotSharp.Abstraction.VectorStorage.Options;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public abstract partial class VectorKnowledgeBase
{
    #region Collection data
    public virtual async Task<bool> CreateCollectionData(string collectionName, KnowledgeCreateModel create)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(create.Text))
        {
            return false;
        }

        var vectorDb = GetVectorDb(create.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var textEmbedding = await GetTextEmbedding(collectionName);
        var vector = await textEmbedding.GetVectorAsync(create.Text);

        var guid = Guid.NewGuid();
        var payload = create.Payload ?? new();

        if (!payload.TryGetValue(KnowledgePayloadName.DataSource, out _))
        {
            payload[KnowledgePayloadName.DataSource] = VectorPayloadValue.BuildStringValue(VectorDataSource.Api);
        }

        return await vectorDb.Upsert(collectionName, guid, vector, create.Text, payload);
    }

    public virtual async Task<bool> DeleteCollectionData(string collectionName, string id, KnowledgeCollectionOptions? options)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return false;
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        return await vectorDb.DeleteCollectionData(collectionName, [guid]);
    }

    public virtual async Task<bool> DeleteCollectionData(string collectionName, KnowledgeCollectionOptions? options)
    {
        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        return await vectorDb.DeleteCollectionAllData(collectionName);
    }

    public virtual async Task<IEnumerable<KnowledgeCollectionData>> GetCollectionData(string collectionName, IEnumerable<string> ids, KnowledgeQueryOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || ids.IsNullOrEmpty())
        {
            return [];
        }

        var pointIds = ids.Select(x => new { Id = x, IsValid = Guid.TryParse(x, out var guid), ParseResult = guid })
                          .Where(x => x.IsValid)
                          .Select(x => x.ParseResult)
                          .ToList();

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return [];
        }

        var queryOptions = options != null ? new VectorQueryOptions
        {
            WithPayload = options.WithPayload,
            WithVector = options.WithVector
        } : null;

        var points = await vectorDb.GetCollectionData(collectionName, pointIds, queryOptions);
        return points.Select(x => new KnowledgeCollectionData
        {
            Id = x.Id,
            Payload = x.Payload,
            Score = x.Score,
            Vector = x.Vector
        });
    }

    public virtual async Task<StringIdPagedItems<KnowledgeCollectionData>> GetPagedCollectionData(string collectionName, KnowledgeFilter filter)
    {
        var vectorDb = GetVectorDb(filter.DbProvider);
        if (vectorDb == null)
        {
            return new StringIdPagedItems<KnowledgeCollectionData>();
        }

        var vectorFilter = new VectorFilter
        {
            WithVector = filter.WithVector,
            FilterGroups = filter.FilterGroups,
            OrderBy = filter.OrderBy,
            Fields = filter.Fields,
            Size = filter.Size,
            StartId = filter.StartId
        };

        var pagedResult = await vectorDb.GetPagedCollectionData(collectionName, vectorFilter);
        return new StringIdPagedItems<KnowledgeCollectionData>
        {
            Count = pagedResult.Count,
            Items = pagedResult.Items.Select(x => new KnowledgeCollectionData
            {
                Id = x.Id,
                Payload = x.Payload,
                Score = x.Score,
                Vector = x.Vector
            }),
            NextId = pagedResult.NextId,
        };
    }

    public virtual async Task<bool> UpdateCollectionData(string collectionName, KnowledgeUpdateModel update)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(update.Text)
            || !Guid.TryParse(update.Id, out var guid))
        {
            return false;
        }

        var vectorDb = GetVectorDb(update.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var found = await vectorDb.GetCollectionData(collectionName, [guid]);
        if (found.IsNullOrEmpty())
        {
            return false;
        }

        var textEmbedding = await GetTextEmbedding(collectionName);
        var vector = await textEmbedding.GetVectorAsync(update.Text);
        var payload = update.Payload ?? new();

        if (!payload.TryGetValue(KnowledgePayloadName.DataSource, out _))
        {
            payload[KnowledgePayloadName.DataSource] = VectorPayloadValue.BuildStringValue(VectorDataSource.Api);
        }

        return await vectorDb.Upsert(collectionName, guid, vector, update.Text, payload);
    }

    public virtual async Task<bool> UpsertCollectionData(string collectionName, KnowledgeUpdateModel update)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(update.Text)
            || !Guid.TryParse(update.Id, out var guid))
        {
            return false;
        }

        var vectorDb = GetVectorDb(update.DbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var found = await vectorDb.GetCollectionData(collectionName, [guid], options: new() { WithVector = true, WithPayload = true });
        if (!found.IsNullOrEmpty())
        {
            if (found.First().Data[KnowledgePayloadName.Text].ToString() == update.Text)
            {
                // Only update payload
                return await vectorDb.Upsert(collectionName, guid, found.First().Vector, update.Text, update.Payload);
            }
        }

        var textEmbedding = await GetTextEmbedding(collectionName);
        var vector = await textEmbedding.GetVectorAsync(update.Text);
        var payload = update.Payload ?? new();

        if (!payload.TryGetValue(KnowledgePayloadName.DataSource, out _))
        {
            payload[KnowledgePayloadName.DataSource] = VectorPayloadValue.BuildStringValue(VectorDataSource.Api);
        }

        return await vectorDb.Upsert(collectionName, guid, vector, update.Text, payload);
    }
    #endregion

    public virtual async Task<IEnumerable<KnowledgeExecuteResult>> ExecuteQuery(string query, string collectionName, KnowledgeExecuteOptions options)
    {
        var vectorDb = GetVectorDb(options.DbProvider);
        if (vectorDb == null)
        {
            return [];
        }

        var textEmbedding = await GetTextEmbedding(collectionName);
        var vector = await textEmbedding.GetVectorAsync(query);

        var searchOptions = new VectorSearchOptions
        {
            Fields = options.Fields,
            FilterGroups = options.FilterGroups,
            Limit = options.Limit,
            Confidence = options.Confidence,
            WithVector = options.WithVector,
            SearchParam = options.SearchParam
        };

        var found = await vectorDb.Search(collectionName, vector, searchOptions);
        return found.Select(x => KnowledgeExecuteResult.CopyFrom(new KnowledgeCollectionData
        {
            Id = x.Id,
            Payload = x.Payload,
            Score = x.Score,
            Vector = x.Vector
        })).ToList();
    }
}
