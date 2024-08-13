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

    public async Task<StringIdPagedItems<KnowledgeCollectionData>> GetKnowledgeCollectionData(string collectionName, KnowledgeFilter filter)
    {
        try
        {
            var db = GetVectorDb();
            return await db.GetCollectionData(collectionName, filter);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting knowledge collection data ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return new StringIdPagedItems<KnowledgeCollectionData>();
        }
    }

    public async Task<IEnumerable<KnowledgeRetrievalResult>> SearchKnowledge(string collectionName, KnowledgeRetrievalOptions options)
    {
        try
        {
            var textEmbedding = GetTextEmbedding();
            var vector = await textEmbedding.GetVectorAsync(options.Text);

            // Vector search
            var db = GetVectorDb();
            var fields = !options.Fields.IsNullOrEmpty() ? options.Fields : new List<string> { KnowledgePayloadName.Text, KnowledgePayloadName.Answer };
            var found = await db.Search(collectionName, vector, fields, limit: options.Limit ?? 5, confidence: options.Confidence ?? 0.5f, withVector: options.WithVector);

            var results = found.Select(x => new KnowledgeRetrievalResult
            {
                Data = x.Data,
                Score = x.Score,
                Vector = x.Vector
            }).ToList();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when searching knowledge ({collectionName}). {ex.Message}\r\n{ex.InnerException}");
            return new List<KnowledgeRetrievalResult>();
        }
    }
}
