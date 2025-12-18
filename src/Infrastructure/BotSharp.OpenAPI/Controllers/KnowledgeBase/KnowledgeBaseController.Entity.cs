using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

public partial class KnowledgeBaseController
{
    /// <summary>
    /// Entity analyis with options
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("knowledge/entity/analyze")]
    public async Task<EntityAnalysisResponse?> EntityAnalyze([FromBody] EntityAnalysisRequest request)
    {
        var analyzer = _services.GetServices<IEntityAnalyzer>()
                                .FirstOrDefault(x => x.Provider.IsEqualTo(request.Provider));

        if (analyzer == null)
        {
            return null;
        }
        return await analyzer.AnalyzeAsync(request.Text, request.Options);
    }

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
