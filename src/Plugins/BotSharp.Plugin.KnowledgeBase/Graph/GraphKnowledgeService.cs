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

    public async Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryOptions? options = null)
    {
        var db = GetGraphDb(options?.Provider);
        var result = await db.ExecuteQueryAsync(query, options);
        return result;
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
