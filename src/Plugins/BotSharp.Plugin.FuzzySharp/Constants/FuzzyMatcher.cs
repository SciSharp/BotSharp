using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Plugin.FuzzySharp.Services;
using BotSharp.Plugin.FuzzySharp.Utils;
using System.Text.RegularExpressions;
using FuzzySharp;

namespace BotSharp.Plugin.FuzzySharp.Constants
{
    public class FuzzyMatcher : ITokenMatcher
    {
        public int Priority => 1; // Lowest priority

        public MatchResult? TryMatch(MatchContext context)
        {
            var match = CheckTypoCorrection(context.ContentSpan, context.Vocabulary, context.Cutoff);
            if (match == null)
            {
                return null;
            }

            return new MatchResult(
                CanonicalForm: match.CanonicalForm,
                DomainTypes: new List<string> { match.DomainType },
                MatchType: MatchReason.TypoCorrection,
                Confidence: match.Confidence);
        }

        /// <summary>
        /// Check typo correction using fuzzy matching with hybrid approach
        /// </summary>
        private TypoCorrectionMatch? CheckTypoCorrection(
           string contentSpan,
           Dictionary<string, HashSet<string>> vocabulary,
           double cutoff)
        {
            // Hybrid filtering parameters
            const double minLengthRatio = 0.4;
            const double maxLengthRatio = 2.5;
            const int allowTokenDiff = 1;
            const int baseCutoff = 60;
            const int strictScoreCutoff = 85;
            const bool useAdaptiveThreshold = true;

            // Normalize and prepare query
            var normalizedToken = Normalize(contentSpan);
            var queryTokenCount = TextTokenizer.SimpleTokenize(normalizedToken).Count;
            var queryLenChars = normalizedToken.Replace(" ", "").Length;

            // Compute adaptive thresholds based on query length
            var (adaptiveCutoff, adaptiveStrictCutoff) = ComputeAdaptiveThresholds(
                queryLenChars, baseCutoff, strictScoreCutoff, useAdaptiveThreshold);

            // Find best match across all domains
            var (bestName, bestType, bestScore) = FindBestMatchAcrossDomains(
                normalizedToken,
                queryTokenCount,
                vocabulary,
                minLengthRatio,
                maxLengthRatio,
                allowTokenDiff,
                adaptiveCutoff,
                adaptiveStrictCutoff);


            if (bestName == null)
            {
                return null;
            }

            // Apply user's cutoff threshold
            if (bestScore < cutoff * 100)
            {
                return null;
            }

            return new TypoCorrectionMatch(
                bestName,
                bestType!,
                Math.Round(bestScore / 100.0, 3));
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

        private (int AdaptiveCutoff, int AdaptiveStrictCutoff) ComputeAdaptiveThresholds(
            int queryLenChars, int scoreCutoff, int strictScoreCutoff, bool useAdaptiveThreshold)
        {
            if (!useAdaptiveThreshold)
            {
                return (scoreCutoff, strictScoreCutoff);
            }

            if (queryLenChars <= 3)
            {
                return (Math.Max(scoreCutoff, 90), 95);
            }
            else if (queryLenChars <= 10)
            {
                return (scoreCutoff, strictScoreCutoff);
            }
            else
            {
                return (Math.Max(scoreCutoff - 5, 55), Math.Max(strictScoreCutoff - 5, 80));
            }
        }

        private (string? BestName, string? BestType, int BestScore) FindBestMatchAcrossDomains(
            string normalizedToken,
            int queryTokenCount,
            Dictionary<string, HashSet<string>> vocabulary,
            double minLengthRatio,
            double maxLengthRatio,
            int allowTokenDiff,
            int adaptiveCutoff,
            int adaptiveStrictCutoff)
        {
            string? bestName = null;
            string? bestType = null;
            int bestScore = 0;

            foreach (var (domainType, terms) in vocabulary)
            {
                var (domainBestName, domainBestScore) = FindBestCandidateInDomain(
                    normalizedToken,
                    queryTokenCount,
                    terms.ToList(),
                    minLengthRatio,
                    maxLengthRatio,
                    allowTokenDiff,
                    adaptiveCutoff,
                    adaptiveStrictCutoff);

                if (domainBestName != null && domainBestScore > bestScore)
                {
                    bestName = domainBestName;
                    bestType = domainType;
                    bestScore = domainBestScore;
                }
            }

            return (bestName, bestType, bestScore);
        }

        private (string? BestName, int BestScore) FindBestCandidateInDomain(
            string normalizedToken,
            int queryTokenCount,
            List<string> candidates,
            double minLengthRatio,
            double maxLengthRatio,
            int allowTokenDiff,
            int adaptiveCutoff,
            int adaptiveStrictCutoff)
        {
            // Step 1: Filter by character length ratio (coarse filter)
            var lengthFiltered = FilterByLengthRatio(candidates, normalizedToken, minLengthRatio, maxLengthRatio);
            if (lengthFiltered.Count == 0)
            {
                return (null, 0);
            }

            // Step 2: Find the best fuzzy match from length-filtered candidates
            int domainBestScore = 0;
            string? domainBestName = null;

            foreach (var candidate in lengthFiltered)
            {
                var normalizedCandidate = Normalize(candidate);
                var score = Fuzz.Ratio(normalizedToken, normalizedCandidate);

                if (score < adaptiveCutoff)
                {
                    continue;
                }

                // Step 3: Apply token count filtering
                if (IsValidMatch(score, queryTokenCount, normalizedCandidate, allowTokenDiff, adaptiveCutoff, adaptiveStrictCutoff))
                {
                    if (score > domainBestScore)
                    {
                        domainBestScore = score;
                        domainBestName = candidate;
                    }
                }
            }

            return (domainBestName, domainBestScore);
        }

        private List<string> FilterByLengthRatio(
            List<string> choices, string queryNormalized, double minLengthRatio, double maxLengthRatio)
        {
            var qLen = queryNormalized.Replace(" ", "").Length;
            if (qLen == 0)
            {
                return new List<string>();
            }

            var kept = new List<string>();
            foreach (var choice in choices)
            {
                var cNormalized = Normalize(choice);
                var cLen = cNormalized.Replace(" ", "").Length;
                if (cLen == 0)
                {
                    continue;
                }

                var lengthRatio = (double)cLen / qLen;
                if (lengthRatio >= minLengthRatio && lengthRatio <= maxLengthRatio)
                {
                    kept.Add(choice);
                }
            }

            return kept;
        }

        private bool IsValidMatch(
            int score,
            int queryTokenCount,
            string normalizedCandidate,
            int allowTokenDiff,
            int adaptiveCutoff,
            int adaptiveStrictCutoff)
        {
            var termTokenCount = TextTokenizer.SimpleTokenize(normalizedCandidate).Count;
            var tokenDiff = Math.Abs(termTokenCount - queryTokenCount);

            if (tokenDiff == 0 && score >= adaptiveCutoff)
            {
                return true;
            }

            if (tokenDiff <= allowTokenDiff && score >= adaptiveStrictCutoff)
            {
                return true;
            }

            return false;
        }
    }
}
