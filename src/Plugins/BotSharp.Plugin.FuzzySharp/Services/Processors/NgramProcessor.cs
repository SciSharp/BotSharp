using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Abstraction.FuzzSharp.Models;
using BotSharp.Plugin.FuzzySharp.Constants;
using BotSharp.Plugin.FuzzySharp.Utils;

namespace BotSharp.Plugin.FuzzySharp.Services.Processors
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
            // Extract content span
            var (contentSpan, spanTokens, contentIndices) = ExtractContentSpan(tokens, startIdx, n);
            if (string.IsNullOrWhiteSpace(contentSpan))
            {
                return null;
            }

            var contentLow = contentSpan.ToLowerInvariant();

            // Try matching in priority order using matchers
            var context = new MatchContext(
                contentSpan, 
                contentLow, 
                startIdx, 
                n,
                vocabulary, 
                domainTermMapping, 
                lookup,
                cutoff, 
                topK);

            foreach (var matcher in _matchers)
            {
                var matchResult = matcher.TryMatch(context);
                if (matchResult != null)
                {
                    return CreateFlaggedItem(matchResult, startIdx, contentSpan, n);
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
        /// Extract content span 
        /// </summary>
        private (string ContentSpan, List<string> Tokens, List<int> ContentIndices) ExtractContentSpan(
            List<string> tokens, 
            int startIdx, 
            int n)
        {
            var span = tokens.Skip(startIdx).Take(n).ToList();
            var indices = Enumerable.Range(startIdx, n).ToList();
            return (string.Join(" ", span), span, indices);
        }
    }
}
