using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Plugin.FuzzySharp.Constants;

namespace BotSharp.Plugin.FuzzySharp.Services.Matching
{
    public class DomainTermMatcher : ITokenMatcher
    {
        public int Priority => 3; // Highest priority

        public MatchResult? TryMatch(MatchContext context)
        {
            if (context.DomainTermMapping.TryGetValue(context.ContentLow, out var match))
            {
                return new MatchResult(
                    CanonicalForm: match.CanonicalForm,
                    DomainTypes: new List<string> { match.DbPath },
                    MatchType: MatchReason.DomainTermMapping,
                    Confidence: 1.0);
            }

            return null;
        }
    }
}
