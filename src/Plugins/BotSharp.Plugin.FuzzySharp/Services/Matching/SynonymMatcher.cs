namespace BotSharp.Plugin.FuzzySharp.Services.Matching;

public class SynonymMatcher : ITokenMatcher
{
    public int Priority => 3; // Highest priority

    public MatchResult? TryMatch(MatchContext context)
    {
        if (context.SynonymMapping.TryGetValue(context.ContentLow, out var match))
        {
            return new MatchResult(
                CanonicalForm: match.CanonicalForm,
                Sources: new List<string> { match.DataSource },
                MatchType: MatchReason.SynonymMatch,
                Confidence: 1.0);
        }

        return null;
    }
}
