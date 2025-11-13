using BotSharp.Plugin.FuzzySharp.FuzzSharp;
using BotSharp.Plugin.FuzzySharp.FuzzSharp.Arguments;
using BotSharp.Plugin.FuzzySharp.FuzzSharp.Models;
using BotSharp.Abstraction.Knowledges;
using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Plugin.FuzzySharp.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BotSharp.Plugin.FuzzySharp.Services;

public class PhraseService : IPhraseService
{
    private readonly ILogger<PhraseService> _logger;
    private readonly IEnumerable<IPhraseCollection> _phraseLoaderServices;
    private readonly INgramProcessor _ngramProcessor;
    private readonly IResultProcessor _resultProcessor;

    public PhraseService(
        ILogger<PhraseService> logger,
        IEnumerable<IPhraseCollection> phraseLoaderServices,
        INgramProcessor ngramProcessor,
        IResultProcessor resultProcessor)
    {
        _logger = logger;
        _phraseLoaderServices = phraseLoaderServices;
        _ngramProcessor = ngramProcessor;
        _resultProcessor = resultProcessor;
    }

    public Task<List<SearchPhrasesResult>> SearchPhrasesAsync(string term)
    {
        var request = BuildTextAnalysisRequest(term);
        var response = AnalyzeTextAsync(request);
        return response.ContinueWith(t =>
        {
            var results = t.Result.Flagged.Select(f => new SearchPhrasesResult
            {
                Token = f.Token,
                Sources = f.Sources,
                CanonicalForm = f.CanonicalForm,
                MatchType = f.MatchType,
                Confidence = f.Confidence
            }).ToList();
            return results;
        });
    }

    private TextAnalysisRequest BuildTextAnalysisRequest(string inputText)
    {
        return new TextAnalysisRequest
        {
            Text = inputText
        };
    }

    /// <summary>
    /// Analyze text for typos and entities using domain-specific vocabulary
    /// </summary>
    private async Task<TextAnalysisResponse> AnalyzeTextAsync(TextAnalysisRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Tokenize the text
            var tokens = TextTokenizer.Tokenize(request.Text);

            // Load vocabulary
            var vocabulary = await LoadAllVocabularyAsync();

            // Load synonym mapping
            var synonymMapping = await LoadAllSynonymMappingAsync();

            // Analyze text
            var flagged = AnalyzeTokens(tokens, vocabulary, synonymMapping, request);

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
        catch (Exception)
        {
            stopwatch.Stop();
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

    public async Task<Dictionary<string, (string DbPath, string CanonicalForm)>> LoadAllSynonymMappingAsync()
    {
        var results = await Task.WhenAll(_phraseLoaderServices.Select(c => c.LoadSynonymMappingAsync()));
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
        Dictionary<string, (string DbPath, string CanonicalForm)> synonymMapping,
        TextAnalysisRequest request)
    {
        // Build lookup table for O(1) exact match lookups (matching Python's build_lookup)
        var lookup = BuildLookup(vocabulary);

        // Process n-grams and find matches
        var flagged = _ngramProcessor.ProcessNgrams(
            tokens,
            vocabulary,
            synonymMapping,
            lookup,
            request.MaxNgram,
            request.Cutoff,
            request.TopK);

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
    private Dictionary<string, (string CanonicalForm, List<string> Sources)> BuildLookup(
        Dictionary<string, HashSet<string>> vocabulary)
    {
        var lookup = new Dictionary<string, (string CanonicalForm, List<string> Sources)>();

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
                    lookup[key] = (term, new List<string> { source });
                }
            }
        }

        return lookup;
    }
}
