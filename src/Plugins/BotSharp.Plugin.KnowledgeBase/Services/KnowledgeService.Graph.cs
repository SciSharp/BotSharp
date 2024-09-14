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
            _logger.LogWarning($"Error when searching graph knowledge (Query: {query}). {ex.Message}\r\n{ex.InnerException}");
            return new GraphSearchResult();
        }
    }
}
