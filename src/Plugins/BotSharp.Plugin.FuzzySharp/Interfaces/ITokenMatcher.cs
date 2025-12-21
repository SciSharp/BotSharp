namespace BotSharp.Plugin.FuzzySharp.Interfaces;

public interface ITokenMatcher
{
    /// <summary>
    /// Try to match a content span and return a match result
    /// </summary>
    /// <param name="context">The matching context containing all necessary information</param>
    /// <returns>Match result if found, null otherwise</returns>
    MatchResult? TryMatch(MatchContext context);

    /// <summary>
    /// Priority of this matcher (higher priority matchers are tried first)
    /// </summary>
    MatchPriority Priority { get; }
}

/// <summary>
/// Context information for token matching
/// </summary>
public record MatchContext(
    string ContentSpan,
    string ContentLow,
    int StartIndex,
    int NgramLength,
    Dictionary<string, HashSet<string>> Vocabulary,
    Dictionary<string, (string DataSource, string CanonicalForm)> SynonymMapping,
    Dictionary<string, (string CanonicalForm, HashSet<string> Sources)> Lookup,
    double Cutoff,
    int TopK);

/// <summary>
/// Result of a token match
/// </summary>
public record MatchResult(
    string CanonicalForm,
    List<string> Sources,
    MatchPriority MatchType,
    double Confidence);
