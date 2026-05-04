using BotSharp.Abstraction.Graph.Options;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public class SementicGraphKnowledgeBase : IKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SementicGraphKnowledgeBase> _logger;

    public SementicGraphKnowledgeBase(
        IServiceProvider services,
        ILogger<SementicGraphKnowledgeBase> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string KnowledgeType => KnowledgeBaseType.SemanticGraph;

    public async Task<IEnumerable<KnowledgeExecuteResult>> ExecuteQuery(string query, string collectionName, KnowledgeExecuteOptions options)
    {
        var results = new List<KnowledgeExecuteResult>();

        try
        {
            var graphDb = GetGraphDb(options?.DbProvider);
            if (graphDb == null)
            {
                _logger.LogWarning($"Cannot find graph db provider '{options?.DbProvider}'.");
                return results;
            }

            var graphExecuteOptions = options as GraphKnowledgeExecuteOptions;
            var graphOptions = new GraphQueryExecuteOptions
            {
                GraphId = graphExecuteOptions?.GraphId,
                Arguments = options?.SearchArguments
            };

            var graphResult = await graphDb.ExecuteQueryAsync(query, graphOptions);

            results = graphResult?.Values?.Select(value => new KnowledgeExecuteResult
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
