using BotSharp.Abstraction.FuzzSharp;
using System.Text.RegularExpressions;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using BotSharp.Plugin.FuzzySharp.Constants;

namespace BotSharp.Plugin.FuzzySharp.Services.Matching
{
    public class FuzzyMatcher : ITokenMatcher
    {
        public int Priority => 1; // Lowest priority

        public MatchResult? TryMatch(MatchContext context)
        {
            var match = CheckTypoCorrection(context.ContentSpan, context.Lookup, context.Cutoff);
            if (match == null)
            {
                return null;
            }

            var (canonicalForm, domainTypes, confidence) = match.Value;
            return new MatchResult(
                CanonicalForm: canonicalForm,
                DomainTypes: domainTypes,
                MatchType: MatchReason.TypoCorrection,
                Confidence: confidence);
        }

        /// <summary>
        /// Check typo correction using fuzzy matching
        /// </summary>
        private (string CanonicalForm, List<string> DomainTypes, double Confidence)? CheckTypoCorrection(
           string contentSpan,
           Dictionary<string, (string CanonicalForm, List<string> DomainTypes)> lookup,
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
                candidate => Normalize(candidate),  // Preprocessor function
                scorer,
                scoreCutoff  // Score cutoff
            );

            if (result == null)
            {
                return null;
            }

            // Get the canonical form and domain types from lookup
            var match = lookup[result.Value];
            return (match.CanonicalForm, match.DomainTypes, Math.Round(result.Score / 100.0, 3));
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
}
