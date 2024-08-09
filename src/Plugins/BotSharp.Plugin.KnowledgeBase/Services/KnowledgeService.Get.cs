namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
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

    public async Task<IEnumerable<KnowledgeRetrievalResult>> SearchKnowledge(KnowledgeRetrievalModel model)
    {
        var textEmbedding = GetTextEmbedding();
        var vector = await textEmbedding.GetVectorAsync(model.Text);

        // Vector search
        var db = GetVectorDb();
        var collection = !string.IsNullOrWhiteSpace(model.Collection) ? model.Collection : KnowledgeCollectionName.BotSharp;
        var fields = !model.Fields.IsNullOrEmpty() ? model.Fields : new List<string> { KnowledgePayloadName.Text, KnowledgePayloadName.Answer };
        var found = await db.Search(collection, vector, fields, limit: model.Limit ?? 5, confidence: model.Confidence ?? 0.5f, withVector: model.WithVector);

        var results = found.Select(x => new KnowledgeRetrievalResult
        {
            Data = x.Data,
            Score = x.Score,
            Vector = x.Vector
        }).ToList();
        return results;
    }
}
