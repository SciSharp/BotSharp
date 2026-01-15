using BotSharp.Abstraction.Graph.Options;

namespace BotSharp.Plugin.KnowledgeBase.Graph;

public partial class GraphKnowledgeService : IGraphKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GraphKnowledgeService> _logger;
    private readonly KnowledgeBaseSettings _settings;

    public GraphKnowledgeService(
        IServiceProvider services,
        ILogger<GraphKnowledgeService> logger,
        KnowledgeBaseSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public async Task<GraphSearchResult> SearchAsync(string query, GraphSearchOptions? options = null)
    {
        try
        {
            var db = GetGraphDb(options?.Provider);
            var result = await db.SearchAsync(query, options);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when searching graph knowledge (Query: {query}).");
            return new GraphSearchResult();
        }
    }


    #region Private methods
    private IGraphDb GetGraphDb(string? provider = null)
    {
        var graphProvider = provider ?? _settings.GraphDb.Provider;
        var db = _services.GetServices<IGraphDb>().FirstOrDefault(x => x.Provider == graphProvider);
        return db;
    }
    #endregion
}
