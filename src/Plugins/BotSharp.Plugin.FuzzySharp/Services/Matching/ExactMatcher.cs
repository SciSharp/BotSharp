using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Plugin.FuzzySharp.Constants;

namespace BotSharp.Plugin.FuzzySharp.Services.Matching
{
    public class ExactMatcher : ITokenMatcher
    {
        public int Priority => 2; // Second highest priority

        public MatchResult? TryMatch(MatchContext context)
        {
            if (context.Lookup.TryGetValue(context.ContentLow, out var match))
            {
                return new MatchResult(
                    CanonicalForm: match.CanonicalForm,
                    DomainTypes: match.DomainTypes,
                    MatchType: MatchReason.ExactMatch,
                    Confidence: 1.0);
            }

            return null;
        }
    }
}
