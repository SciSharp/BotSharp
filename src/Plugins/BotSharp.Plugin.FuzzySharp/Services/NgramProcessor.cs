using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Abstraction.FuzzSharp.Models;
using BotSharp.Plugin.FuzzySharp.Constants;
using BotSharp.Plugin.FuzzySharp.Utils;

namespace BotSharp.Plugin.FuzzySharp.Services
{
    public class NgramProcessor : INgramProcessor
    {
        private readonly List<ITokenMatcher> _matchers;

        public NgramProcessor(IEnumerable<ITokenMatcher> matchers)
        {
            // Sort matchers by priority (highest first)
            _matchers = matchers.OrderByDescending(m => m.Priority).ToList();
        }

        public List<FlaggedItem> ProcessNgrams(
            List<string> tokens,
            Dictionary<string, HashSet<string>> vocabulary,
            Dictionary<string, (string DbPath, string CanonicalForm)> domainTermMapping,
            Dictionary<string, (string CanonicalForm, List<string> DomainTypes)> lookup,
            int maxNgram,
            double cutoff,
            int topK)
        {
            var flagged = new List<FlaggedItem>();

            // Process n-grams from largest to smallest
            for (int n = maxNgram; n >= 1; n--)
            {
                for (int i = 0; i <= tokens.Count - n; i++)
                {
                    var item = ProcessSingleNgram(
                        tokens,
                        i,
                        n,
                        vocabulary,
                        domainTermMapping,
                        lookup,
                        cutoff,
                        topK);

                    if (item != null)
                    {
                        flagged.Add(item);
                    }
                }
            }

            return flagged;
        }

        /// <summary>
        /// Process a single n-gram at the specified position
        /// </summary>
        private FlaggedItem? ProcessSingleNgram(
            List<string> tokens,
            int startIdx,
            int n,
            Dictionary<string, HashSet<string>> vocabulary,
            Dictionary<string, (string DbPath, string CanonicalForm)> domainTermMapping,
            Dictionary<string, (string CanonicalForm, List<string> DomainTypes)> lookup,
            double cutoff,
            int topK)
        {
            // Skip if starting with separator
            if (IsSeparatorToken(tokens[startIdx]))
            {
                return null;
            }

            // Extract content span (remove leading/trailing separators)
            var (contentSpan, contentIndices) = ExtractContentSpan(tokens, startIdx, n);

            if (string.IsNullOrWhiteSpace(contentSpan) || contentIndices.Count == 0)
            {
                return null;
            }

            var startIndex = contentIndices[0];
            var contentLow = contentSpan.ToLowerInvariant();

            // Before fuzzy matching, skip if any contiguous sub-span has an exact or mapped match
            // This prevents "with pending dispatch" from being fuzzy-matched when "pending dispatch" is exact
            if (n > 1 && HasExactSubspanMatch(contentSpan, lookup, domainTermMapping))
            {
                return null;
            }

            // Try matching in priority order using matchers
            var context = new MatchContext(
                contentSpan, contentLow, startIndex, n,
                vocabulary, domainTermMapping, lookup,
                cutoff, topK);

            foreach (var matcher in _matchers)
            {
                var matchResult = matcher.TryMatch(context);
                if (matchResult != null)
                {
                    return CreateFlaggedItem(matchResult, startIndex, contentSpan, n);
                }
            }

            return null;
        }

        /// <summary>
        /// Create a FlaggedItem from a MatchResult
        /// </summary>
        private FlaggedItem CreateFlaggedItem(
            MatchResult matchResult,
            int startIndex,
            string contentSpan,
            int ngramLength)
        {
            return new FlaggedItem
            {
                Index = startIndex,
                Token = contentSpan,
                DomainTypes = matchResult.DomainTypes,
                MatchType = matchResult.MatchType,
                CanonicalForm = matchResult.CanonicalForm,
                Confidence = matchResult.Confidence,
                NgramLength = ngramLength
            };
        }

        /// <summary>
        /// Check if token is a separator
        /// </summary>
        private bool IsSeparatorToken(string token)
        {
            return token.Length == 1 && TextConstants.SeparatorChars.Contains(token[0]);
        }

        /// <summary>
        /// Extract content span by removing leading and trailing separators
        /// </summary>
        private (string ContentSpan, List<int> ContentIndices) ExtractContentSpan(
            List<string> tokens, int startIdx, int n)
        {
            var span = tokens.Skip(startIdx).Take(n).ToList();
            var indices = Enumerable.Range(startIdx, n).ToList();

            // Remove leading separators
            while (span.Count > 0 && IsSeparatorToken(span[0]))
            {
                span.RemoveAt(0);
                indices.RemoveAt(0);
            }

            // Remove trailing separators
            while (span.Count > 0 && IsSeparatorToken(span[^1]))
            {
                span.RemoveAt(span.Count - 1);
                indices.RemoveAt(indices.Count - 1);
            }

            return (string.Join(" ", span), indices);
        }

        /// <summary>
        /// Check whether any contiguous sub-span of content_span is an exact hit
        /// </summary>
        private bool HasExactSubspanMatch(
            string contentSpan,
            Dictionary<string, (string CanonicalForm, List<string> DomainTypes)> lookup,
            Dictionary<string, (string DbPath, string CanonicalForm)> domainTermMapping)
        {
            if (string.IsNullOrWhiteSpace(contentSpan))
            {
                return false;
            }

            var contentTokens = TextTokenizer.SimpleTokenize(contentSpan);

            // Try all contiguous sub-spans
            for (int subN = contentTokens.Count; subN > 0; subN--)
            {
                for (int subI = 0; subI <= contentTokens.Count - subN; subI++)
                {
                    var subSpan = string.Join(" ", contentTokens.Skip(subI).Take(subN));
                    var subSpanLow = subSpan.ToLowerInvariant();

                    // Check if it's in domain term mapping
                    if (domainTermMapping.ContainsKey(subSpanLow))
                    {
                        return true;
                    }

                    // Check if it's an exact match in lookup table
                    if (lookup.ContainsKey(subSpanLow))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
