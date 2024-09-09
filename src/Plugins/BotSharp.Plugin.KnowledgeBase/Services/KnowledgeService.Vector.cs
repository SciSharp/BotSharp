namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    #region Collection
    public async Task<bool> CreateVectorCollection(string collectionName, int dimension)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return false;
            }

            var db = GetVectorDb();
            return await db.CreateCollection(collectionName, dimension);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when creating a vector collection ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetVectorCollections()
    {
        try
        {
            var db = GetVectorDb();
            return await db.GetCollections();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting vector db collections. {ex.Message}\r\n{ex.InnerException}");
            return Enumerable.Empty<string>();
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

            var db = GetVectorDb();
            return await db.DeleteCollection(collectionName);
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
            return await db.Upsert(collectionName, guid, vector, create.Text, create.Payload);
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
            return await db.Upsert(collectionName, guid, vector, update.Text, update.Payload);
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
            return await db.DeleteCollectionData(collectionName, guid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when deleting vector collection data ({collectionName}-{id}). {ex.Message}\r\n{ex.InnerException}");
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
            return new List<VectorSearchResult>();
        }
    }
    #endregion
}
