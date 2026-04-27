using BotSharp.Abstraction.Graph.Options;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public class GraphKnowledgeOrchestrator : IKnowledgeOrchestrator
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GraphKnowledgeOrchestrator> _logger;

    public GraphKnowledgeOrchestrator(
        IServiceProvider services,
        ILogger<GraphKnowledgeOrchestrator> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string KnowledgeType => KnowledgeBaseType.SemanticGraph;

    public async Task<IEnumerable<KnowledgeSearchResult>> Search(string query, string collectionName, KnowledgeSearchOptions options)
    {
        var results = new List<KnowledgeSearchResult>();

        try
        {
            var graphDb = GetGraphDb(options?.DbProvider);
            if (graphDb == null)
            {
                _logger.LogWarning($"Cannot find graph db provider '{options?.DbProvider}'.");
                return results;
            }

            var graphSearchOptions = options as GraphKnowledgeSearchOptions;
            var graphOptions = new GraphQueryExecuteOptions
            {
                GraphId = graphSearchOptions?.GraphId,
                Arguments = options?.SearchArguments
            };

            var graphResult = await graphDb.ExecuteQueryAsync(query, graphOptions);

            results = graphResult?.Values?.Select(value => new KnowledgeSearchResult
            {
                Id = Guid.NewGuid().ToString(),
                Payload = value.ToDictionary(kvp => kvp.Key, kvp => new VectorPayloadValue(kvp.Value))
            })?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when searching graph knowledge. (Query: {query})");
        }

        return results;
    }

    #region Private methods
    private IGraphDb? GetGraphDb(string? provider = null)
    {
        var db = _services.GetServices<IGraphDb>().FirstOrDefault(x => x.Provider.IsEqualTo(provider));
        return db;
    }
    #endregion
}
