namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
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

    public async Task<StringIdPagedItems<VectorSearchResult>> GetPagedVectorCollectionData(string collectionName, VectorFilter filter)
    {
        try
        {
            var db = GetVectorDb();
            var pagedResult =  await db.GetPagedCollectionData(collectionName, filter);
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

    public async Task<GraphSearchResult> SearchGraphKnowledge(string query, GraphSearchOptions options)
    {
        try
        {
            var db = GetGraphDb();
            var found = await db.Search(query, options);
            return new GraphSearchResult
            {
                Result = found.Result
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when searching graph knowledge (Query: {query}). {ex.Message}\r\n{ex.InnerException}");
            return new GraphSearchResult();
        }
    }

    public async Task<KnowledgeSearchResult> SearchKnowledge(string query, string collectionName, VectorSearchOptions vectorOptions, GraphSearchOptions graphOptions)
    {
        try
        {
            var textEmbedding = GetTextEmbedding(collectionName);
            var vector = await textEmbedding.GetVectorAsync(query);

            var vectorDb = GetVectorDb();
            var vectorRes = await vectorDb.Search(collectionName, vector, vectorOptions.Fields, limit: vectorOptions.Limit ?? 5,
                            confidence: vectorOptions.Confidence ?? 0.5f, withVector: vectorOptions.WithVector);

            var graphDb = GetGraphDb();
            var graphRes = await graphDb.Search(query, graphOptions);
            return new KnowledgeSearchResult
            {
                VectorResult = vectorRes.Select(x => VectorSearchResult.CopyFrom(x)),
                GraphResult = new GraphSearchResult { Result = graphRes.Result }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when searching knowledge (Vector collection: {collectionName}) (Query: {query}). {ex.Message}\r\n{ex.InnerException}");
            return new KnowledgeSearchResult();
        }
    }
}
