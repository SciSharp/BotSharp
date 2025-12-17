using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BotSharp.Plugin.FuzzySharp.Services;

public class FuzzySharpNERAnalyzer : INERAnalyzer
{
    private readonly ILogger<FuzzySharpNERAnalyzer> _logger;
    private readonly IEnumerable<INERDataLoader> _tokenDataLoaders;
    private readonly INgramProcessor _ngramProcessor;
    private readonly IResultProcessor _resultProcessor;

    public FuzzySharpNERAnalyzer(
        ILogger<FuzzySharpNERAnalyzer> logger,
        IEnumerable<INERDataLoader> tokenDataLoaders,
        INgramProcessor ngramProcessor,
        IResultProcessor resultProcessor)
    {
        _logger = logger;
        _tokenDataLoaders = tokenDataLoaders;
        _ngramProcessor = ngramProcessor;
        _resultProcessor = resultProcessor;
    }

    public string Provider => "fuzzy-sharp";

    public async Task<NERResponse> AnalyzeAsync(string text, NEROptions? options = null)
    {
        var response = new NERResponse();

        try
        {
            var result = await AnalyzeTextAsync(text, options);
            return new NERResponse
            {
                Success = true,
                Results = result?.FlaggedItems?.Select(f => new NERResult
                {
                    Token = f.Token,
                    CanonicalText = f.CanonicalForm,
                    Data = new Dictionary<string, object>
                    {
                        ["sources"] = f.Sources,
                        ["match_type"] = f.MatchType.Name,
                        ["confidence"] = f.Confidence
                    }
                })?.ToList() ?? []
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when tokenize in {Provider}: {text}.");
            response.ErrorMsg = ex.Message;
            return response;
        }
    }

    /// <summary>
    /// Analyze text for typos and entities using domain-specific vocabulary
    /// </summary>
    private async Task<TokenAnalysisResponse> AnalyzeTextAsync(string text, NEROptions? options = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Tokenize the text
            var tokens = TokenHelper.Tokenize(text);

            // Load vocabulary
            var vocabulary = await LoadAllVocabularyAsync(options?.DataProviders);

            // Load synonym mapping
            var synonymMapping = await LoadAllSynonymMappingAsync(options?.DataProviders);

            // Analyze text
            var flaggedItems = AnalyzeTokens(tokens, vocabulary, synonymMapping, options);

            stopwatch.Stop();

            var response = new TokenAnalysisResponse
            {
                Original = text,
                Tokens = tokens,
                FlaggedItems = flaggedItems,
                ProcessingTimeMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
            };

            _logger.LogInformation(
                $"Text analysis completed in {response.ProcessingTimeMs}ms | " +
                $"Text length: {text.Length} chars | " +
                $"Flagged items: {flaggedItems.Count}");

            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            throw;
        }
    }

    public async Task<Dictionary<string, HashSet<string>>> LoadAllVocabularyAsync(IEnumerable<string>? dataProviders = null)
    {
        var dataLoaders = _tokenDataLoaders.Where(x => dataProviders == null || dataProviders.Contains(x.Provider));
        var results = await Task.WhenAll(dataLoaders.Select(c => c.LoadVocabularyAsync()));
        var merged = new Dictionary<string, HashSet<string>>();

        foreach (var dict in results)
        {
            foreach (var kvp in dict)
            {
                if (!merged.TryGetValue(kvp.Key, out var set))
                {
                    merged[kvp.Key] = new HashSet<string>(kvp.Value);
                }
                else
                {
                    set.UnionWith(kvp.Value);
                }
            }
        }

        return merged;
    }

    public async Task<Dictionary<string, (string DbPath, string CanonicalForm)>> LoadAllSynonymMappingAsync(IEnumerable<string>? dataProviders = null)
    {
        var dataLoaders = _tokenDataLoaders.Where(x => dataProviders == null || dataProviders.Contains(x.Provider));
        var results = await Task.WhenAll(dataLoaders.Select(c => c.LoadSynonymMappingAsync()));
        var merged = new Dictionary<string, (string DbPath, string CanonicalForm)>();

        foreach (var dict in results)
        {
            foreach (var kvp in dict)
            {
                merged[kvp.Key] = kvp.Value; // later entries override earlier ones
            }
        }

        return merged;
    }

    /// <summary>
    /// Analyze tokens for typos and entities
    /// </summary>
    private List<FlaggedTokenItem> AnalyzeTokens(
        List<string> tokens,
        Dictionary<string, HashSet<string>> vocabulary,
        Dictionary<string, (string DataSource, string CanonicalForm)> synonymMapping,
        NEROptions? options)
    {
        // Build lookup table for O(1) exact match lookups (matching Python's build_lookup)
        var lookup = BuildLookup(vocabulary);

        // Process n-grams and find matches
        var flagged = _ngramProcessor.ProcessNgrams(
            tokens,
            vocabulary,
            synonymMapping,
            lookup,
            options?.MaxNgram ?? 5,
            options?.Cutoff ?? 0.82,
            options?.TopK ?? 5);

        // Process results: deduplicate and sort
        return _resultProcessor.ProcessResults(flagged);
    }

    /// <summary>
    /// Build a lookup dictionary mapping lowercase terms to their canonical form and sources.
    /// This is a performance optimization - instead of iterating through all sources for each lookup,
    /// we build a flat dictionary once at the start.
    ///
    /// Matches Python's build_lookup() function.
    /// </summary>
    private Dictionary<string, (string CanonicalForm, HashSet<string> Sources)> BuildLookup(
        Dictionary<string, HashSet<string>> vocabulary)
    {
        var lookup = new Dictionary<string, (string CanonicalForm, HashSet<string> Sources)>();

        foreach (var (source, terms) in vocabulary)
        {
            foreach (var term in terms)
            {
                var key = term.ToLowerInvariant();
                if (lookup.TryGetValue(key, out var existing))
                {
                    // Term already exists - add this source to the list if not already there
                    if (!existing.Sources.Contains(source))
                    {
                        existing.Sources.Add(source);
                    }
                }
                else
                {
                    // New term - create entry with single source in list
                    lookup[key] = (term, new HashSet<string> { source });
                }
            }
        }

        return lookup;
    }
}
