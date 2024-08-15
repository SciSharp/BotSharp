namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<IEnumerable<string>> GetKnowledgeCollections()
    {
        try
        {
            var db = GetVectorDb();
            return await db.GetCollections();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting knowledge collections. {ex.Message}\r\n{ex.InnerException}");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<StringIdPagedItems<KnowledgeSearchResult>> GetKnowledgeCollectionData(string collectionName, KnowledgeFilter filter)
    {
        try
        {
            var db = GetVectorDb();
            var pagedResult =  await db.GetCollectionData(collectionName, filter);
            return new StringIdPagedItems<KnowledgeSearchResult>
            {
                Count = pagedResult.Count,
                Items = pagedResult.Items.Select(x => KnowledgeSearchResult.CopyFrom(x)),
                NextId = pagedResult.NextId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting knowledge collection data ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return new StringIdPagedItems<KnowledgeSearchResult>();
        }
    }

    public async Task<IEnumerable<KnowledgeSearchResult>> SearchKnowledge(string collectionName, KnowledgeSearchOptions options)
    {
        try
        {
            var textEmbedding = GetTextEmbedding();
            var vector = await textEmbedding.GetVectorAsync(options.Text);

            // Vector search
            var db = GetVectorDb();
            var found = await db.Search(collectionName, vector, options.Fields, limit: options.Limit ?? 5, confidence: options.Confidence ?? 0.5f, withVector: options.WithVector);

            var results = found.Select(x => KnowledgeSearchResult.CopyFrom(x)).ToList();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when searching knowledge ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return new List<KnowledgeSearchResult>();
        }
    }
}
