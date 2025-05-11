namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
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
            _logger.LogWarning(ex, $"Error when searching graph knowledge (Query: {query}).");
            return new GraphSearchResult();
        }
    }
}
