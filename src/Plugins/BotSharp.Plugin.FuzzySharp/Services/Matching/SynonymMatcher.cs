namespace BotSharp.Plugin.FuzzySharp.Services.Matching;

public class SynonymMatcher : ITokenMatcher
{
    public MatchPriority Priority => MatchReason.SynonymMatch; // Highest priority

    public MatchResult? TryMatch(MatchContext context)
    {
        if (context.SynonymMapping.TryGetValue(context.ContentLow, out var match))
        {
            return new MatchResult(
                CanonicalForm: match.CanonicalForm,
                Sources: new List<string> { match.DataSource },
                MatchType: Priority,
                Confidence: 1.0);
        }

        return null;
    }
}
