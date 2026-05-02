using BotSharp.Abstraction.Entity;
using BotSharp.Abstraction.Entity.Models;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public class TaxonomyKnowledgeBase : IKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TaxonomyKnowledgeBase> _logger;

    public TaxonomyKnowledgeBase(
        IServiceProvider services,
        ILogger<TaxonomyKnowledgeBase> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string KnowledgeType => KnowledgeBaseType.Taxonomy;

    public async Task<IEnumerable<KnowledgeExecuteResult>> ExecuteQuery(string query, string collectionName, KnowledgeExecuteOptions options)
    {
        var results = new List<KnowledgeExecuteResult>();

        try
        {
            var analyzer = _services.GetServices<IEntityAnalyzer>()
                .FirstOrDefault(x => x.Provider.IsEqualTo(options.DbProvider));

            if (analyzer == null)
            {
                _logger.LogWarning($"Cannot find entity analyzer with provider '{options.DbProvider}'.");
                return results;
            }

            var taxonomyOptions = options as TaxonomyKnowledgeSearchOptions;
            var analysisOptions = new EntityAnalysisOptions
            {
                DataProviders = taxonomyOptions?.DataProviders,
                Cutoff = options.Confidence.HasValue ? (double)options.Confidence.Value : null,
                TopK = options.Limit,
                MaxNgram = taxonomyOptions?.MaxNgram
            };

            var response = await analyzer.AnalyzeAsync(query, analysisOptions);

            if (!response.Success || response.Results == null)
            {
                return results;
            }

            foreach (var result in response.Results)
            {
                var payload = new Dictionary<string, VectorPayloadValue>
                {
                    ["token"] = new VectorPayloadValue(result.Token)
                };

                if (!string.IsNullOrEmpty(result.CanonicalText))
                {
                    payload["canonical_text"] = new VectorPayloadValue(result.CanonicalText);
                }

                foreach (var kvp in result.Data)
                {
                    if (!payload.ContainsKey(kvp.Key))
                    {
                        payload[kvp.Key] = new VectorPayloadValue(kvp.Value);
                    }
                }

                double confidence = 0d;
                if (result.Data.TryGetValue("confidence", out var conf))
                {
                    if (conf is double d)
                    {
                        confidence = d;
                    }
                    else if (conf is float f)
                    {
                        confidence = f;
                    }
                }

                results.Add(new KnowledgeExecuteResult
                {
                    Id = Guid.NewGuid().ToString(),
                    Payload = payload,
                    Score = confidence
                });
            }

            if (options.Limit.HasValue)
            {
                results = results
                    .OrderByDescending(x => x.Score)
                    .Take(options.Limit.Value)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when searching taxonomy knowledge ({query}).");
        }

        return results;
    }
}
