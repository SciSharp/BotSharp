namespace BotSharp.Plugin.FuzzySharp.Constants;

public static class MatchReason
{
    /// <summary>
    /// Token matched a synonym term (e.g., HVAC -> Air Conditioning/Heating)
    /// </summary>
    public static MatchPriority SynonymMatch = new(3, "synonym_match");

    /// <summary>
    /// Token exactly matched a vocabulary entry
    /// </summary>
    public static MatchPriority ExactMatch = new(2, "exact_match");

    /// <summary>
    /// Token was flagged as a potential typo and a correction was suggested
    /// </summary>
    public static MatchPriority FuzzyMatch = new(1, "fuzzy_match");
}
