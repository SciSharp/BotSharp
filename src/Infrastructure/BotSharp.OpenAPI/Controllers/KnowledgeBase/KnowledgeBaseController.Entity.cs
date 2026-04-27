namespace BotSharp.OpenAPI.Controllers;

public partial class KnowledgeBaseController
{
    /// <summary>
    /// Get entity analyzers
    /// </summary>
    /// <returns></returns>
    [HttpGet("knowledge/entity/analyzers")]
    public IEnumerable<string> GetEntityAnalyzers()
    {
        var analyzers = _services.GetServices<IEntityAnalyzer>();
        return analyzers.Select(x => x.Provider);
    }

    /// <summary>
    /// Get entity data providers
    /// </summary>
    /// <returns></returns>
    [HttpGet("knowledge/entity/data-providers")]
    public IEnumerable<string> GetEntityDataProviders()
    {
        var dataLoaders = _services.GetServices<IEntityDataLoader>();
        return dataLoaders.Select(x => x.Provider);
    }
}
