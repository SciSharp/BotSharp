using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

public partial class KnowledgeBaseController
{
    /// <summary>
    /// NER analyis with options
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("knowledge/NER/analyze")]
    public async Task<NERResponse?> NERAnalyze([FromBody] NERAnalysisRequest request)
    {
        var analyzer = _services.GetServices<INERAnalyzer>()
                                .FirstOrDefault(x => x.Provider.IsEqualTo(request.Provider));

        if (analyzer == null)
        {
            return null;
        }
        return await analyzer.AnalyzeAsync(request.Text, request.Options);
    }

    /// <summary>
    /// Get NER analyzers
    /// </summary>
    /// <returns></returns>
    [HttpGet("knowledge/NER/analyzers")]
    public IEnumerable<string> GetNERAnalyzers()
    {
        var analyzers = _services.GetServices<INERAnalyzer>();
        return analyzers.Select(x => x.Provider);
    }

    /// <summary>
    /// Get NER data providers
    /// </summary>
    /// <returns></returns>
    [HttpGet("knowledge/NER/data-providers")]
    public IEnumerable<string> GetNERDataProviders()
    {
        var dataLoaders = _services.GetServices<INERDataLoader>();
        return dataLoaders.Select(x => x.Provider);
    }
}
