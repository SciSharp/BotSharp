using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Abstraction.FuzzSharp.Arguments;
using BotSharp.Abstraction.FuzzSharp.Models;
using BotSharp.Abstraction.Knowledges;
using BotSharp.Plugin.FuzzySharp.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BotSharp.Plugin.FuzzySharp.Services;

public class TextAnalysisService : ITextAnalysisService
{
    private readonly ILogger<TextAnalysisService> _logger;
    private readonly IEnumerable<IPhraseCollection> _phraseLoaderServices;
    private readonly INgramProcessor _ngramProcessor;
    private readonly IResultProcessor _resultProcessor;

    public TextAnalysisService(
        ILogger<TextAnalysisService> logger,
        IEnumerable<IPhraseCollection> phraseLoaderServices,
        INgramProcessor ngramProcessor,
        IResultProcessor resultProcessor)
    {
        _logger = logger;
        _phraseLoaderServices = phraseLoaderServices;
        _ngramProcessor = ngramProcessor;
        _resultProcessor = resultProcessor;
    }

    /// <summary>
    /// Analyze text for typos and entities using domain-specific vocabulary
    /// </summary>
    public async Task<TextAnalysisResponse> AnalyzeTextAsync(TextAnalysisRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Tokenize the text
            var tokens = TextTokenizer.Tokenize(request.Text);

            // Load vocabulary
            var vocabulary = await LoadAllVocabularyAsync();

            // Load domain term mapping
            var domainTermMapping = await LoadAllDomainTermMappingAsync();

            // Analyze text
            var flagged = AnalyzeTokens(tokens, vocabulary, domainTermMapping, request);

            stopwatch.Stop();

            var response = new TextAnalysisResponse
            {
                Original = request.Text,
                Flagged = flagged,
                ProcessingTimeMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
            };

            if (request.IncludeTokens)
            {
                response.Tokens = tokens;
            }

            _logger.LogInformation(
                $"Text analysis completed in {response.ProcessingTimeMs}ms | " +
                $"Text length: {request.Text.Length} chars | " +
                $"Flagged items: {flagged.Count}");

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, $"Error analyzing text after {stopwatch.Elapsed.TotalMilliseconds}ms");
            throw;
        }
    }

    public async Task<Dictionary<string, HashSet<string>>> LoadAllVocabularyAsync()
    {
        var results = await Task.WhenAll(_phraseLoaderServices.Select(c => c.LoadVocabularyAsync()));
        var merged = new Dictionary<string, HashSet<string>>();

        foreach (var dict in results)
        {
            foreach (var kvp in dict)
            {
                if (!merged.TryGetValue(kvp.Key, out var set))
                    merged[kvp.Key] = new HashSet<string>(kvp.Value);
                else
                    set.UnionWith(kvp.Value);
            }
        }

        return merged;
    }

    public async Task<Dictionary<string, (string DbPath, string CanonicalForm)>> LoadAllDomainTermMappingAsync()
    {
        var results = await Task.WhenAll(_phraseLoaderServices.Select(c => c.LoadDomainTermMappingAsync()));
        var merged = new Dictionary<string, (string DbPath, string CanonicalForm)>();

        foreach (var dict in results)
        {
            foreach (var kvp in dict)
                merged[kvp.Key] = kvp.Value; // later entries override earlier ones
        }

        return merged;
    }

    /// <summary>
    /// Analyze tokens for typos and entities
    /// </summary>
    private List<FlaggedItem> AnalyzeTokens(
        List<string> tokens,
        Dictionary<string, HashSet<string>> vocabulary,
        Dictionary<string, (string DbPath, string CanonicalForm)> domainTermMapping,
        TextAnalysisRequest request)
    {
        // Build lookup table for O(1) exact match lookups (matching Python's build_lookup)
        var lookup = BuildLookup(vocabulary);

        // Process n-grams and find matches
        var flagged = _ngramProcessor.ProcessNgrams(
            tokens,
            vocabulary,
            domainTermMapping,
            lookup,
            request.MaxNgram,
            request.Cutoff,
            request.TopK);

        // Process results: deduplicate and sort
        return _resultProcessor.ProcessResults(flagged);
    }

    /// <summary>
    /// Build a lookup dictionary mapping lowercase terms to their canonical form and domain types.
    /// This is a performance optimization - instead of iterating through all domains for each lookup,
    /// we build a flat dictionary once at the start.
    ///
    /// Matches Python's build_lookup() function.
    /// </summary>
    private Dictionary<string, (string CanonicalForm, List<string> DomainTypes)> BuildLookup(
        Dictionary<string, HashSet<string>> vocabulary)
    {
        var lookup = new Dictionary<string, (string CanonicalForm, List<string> DomainTypes)>();

        foreach (var (domainType, terms) in vocabulary)
        {
            foreach (var term in terms)
            {
                var key = term.ToLowerInvariant();
                if (lookup.TryGetValue(key, out var existing))
                {
                    // Term already exists - add this domain type to the list if not already there
                    if (!existing.DomainTypes.Contains(domainType))
                    {
                        existing.DomainTypes.Add(domainType);
                    }
                }
                else
                {
                    // New term - create entry with single type in list
                    lookup[key] = (term, new List<string> { domainType });
                }
            }
        }

        return lookup;
    }
}
