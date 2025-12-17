namespace BotSharp.Plugin.FuzzySharp.Services.Matching;

public class ExactMatcher : ITokenMatcher
{
    public MatchPriority Priority => MatchReason.ExactMatch; // Second highest priority

    public MatchResult? TryMatch(MatchContext context)
    {
        if (context.Lookup.TryGetValue(context.ContentLow, out var match))
        {
            return new MatchResult(
                CanonicalForm: match.CanonicalForm,
                Sources: match.Sources.ToList(),
                MatchType: Priority,
                Confidence: 1.0);
        }

        return null;
    }
}
