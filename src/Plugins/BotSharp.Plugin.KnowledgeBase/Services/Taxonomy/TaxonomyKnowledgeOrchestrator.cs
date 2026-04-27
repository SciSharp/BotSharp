using BotSharp.Abstraction.Entity;
using BotSharp.Abstraction.Entity.Models;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public class TaxonomyKnowledgeOrchestrator : IKnowledgeOrchestrator
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TaxonomyKnowledgeOrchestrator> _logger;

    public TaxonomyKnowledgeOrchestrator(
        IServiceProvider services,
        ILogger<TaxonomyKnowledgeOrchestrator> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string KnowledgeType => KnowledgeBaseType.Taxonomy;

    public async Task<IEnumerable<KnowledgeSearchResult>> Search(string query, string collectionName, KnowledgeSearchOptions options)
    {
        var results = new List<KnowledgeSearchResult>();

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
                    ["token"] = VectorPayloadValue.BuildStringValue(result.Token)
                };

                if (!string.IsNullOrEmpty(result.CanonicalText))
                {
                    payload["canonical_text"] = VectorPayloadValue.BuildStringValue(result.CanonicalText);
                }

                foreach (var kvp in result.Data)
                {
                    if (!payload.ContainsKey(kvp.Key))
                    {
                        payload[kvp.Key] = BuildPayloadValue(kvp.Value);
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

                results.Add(new KnowledgeSearchResult
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

    private VectorPayloadValue BuildPayloadValue(object value)
    {
        return value switch
        {
            string s => VectorPayloadValue.BuildStringValue(s),
            double d => VectorPayloadValue.BuildDoubleValue(d),
            float f => VectorPayloadValue.BuildDoubleValue(f),
            long l => VectorPayloadValue.BuildIntegerValue(l),
            int i => VectorPayloadValue.BuildIntegerValue(i),
            short sh => VectorPayloadValue.BuildIntegerValue(sh),
            byte b => VectorPayloadValue.BuildIntegerValue(b),
            bool bl => VectorPayloadValue.BuildBooleanValue(bl),
            DateTime dt => VectorPayloadValue.BuildDatetimeValue(dt),
            _ => VectorPayloadValue.BuildUnkownValue(value)
        };
    }
}
