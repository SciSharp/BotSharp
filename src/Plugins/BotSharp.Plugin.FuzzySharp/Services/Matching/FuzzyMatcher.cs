using System.Text.RegularExpressions;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;

namespace BotSharp.Plugin.FuzzySharp.Services.Matching;

public class FuzzyMatcher : ITokenMatcher
{
    public MatchPriority Priority => MatchReason.FuzzyMatch; // Lowest priority

    public MatchResult? TryMatch(MatchContext context)
    {
        var match = FuzzyMatch(context.ContentSpan, context.Lookup, context.Cutoff);
        if (match == null)
        {
            return null;
        }

        var (canonicalForm, sources, confidence) = match.Value;
        return new MatchResult(
            CanonicalForm: canonicalForm,
            Sources: sources,
            MatchType: Priority,
            Confidence: confidence);
    }

    /// <summary>
    /// Check typo correction using fuzzy matching
    /// </summary>
    private (string CanonicalForm, List<string> Sources, double Confidence)? FuzzyMatch(
       string contentSpan,
       Dictionary<string, (string CanonicalForm, HashSet<string> Sources)> lookup,
       double cutoff)
    {
        // Convert cutoff to 0-100 scale for FuzzySharp
        var scoreCutoff = (int)(cutoff * 100);

        // Get all candidates from lookup
        var candidates = lookup.Keys.ToList();

        // Find best match using ExtractOne
        var scorer = ScorerCache.Get<DefaultRatioScorer>();
        var result = Process.ExtractOne(
            contentSpan,
            candidates,
            candidate => Normalize(candidate), // Preprocessor function
            scorer,
            scoreCutoff // Score cutoff
        );

        // Get the canonical form and sources from lookup
        if (result == null || !lookup.TryGetValue(result.Value, out var match))
        {
            return null;
        }

        return (match.CanonicalForm, match.Sources.ToList(), Math.Round(result.Score / 100.0, 3));
    }

    /// <summary>
    /// Normalize text for fuzzy matching comparison
    /// - Replaces all non-word characters (except apostrophes) with spaces
    /// - Converts to lowercase
    /// - Collapses multiple spaces into single space
    /// - Trims leading/trailing whitespace
    /// Example: "Test-Value (123)" → "test value 123"
    /// </summary>
    /// <param name="text">Text to normalize</param>
    /// <returns>Normalized text suitable for fuzzy matching</returns>
    private string Normalize(string text)
    {
        // Replace non-word characters (except apostrophes) with spaces
        var normalized = Regex.Replace(text, @"[^\w']+", " ", RegexOptions.IgnoreCase);
        // Convert to lowercase, collapse multiple spaces, and trim
        return Regex.Replace(normalized.ToLowerInvariant(), @"\s+", " ").Trim();
    }
}
