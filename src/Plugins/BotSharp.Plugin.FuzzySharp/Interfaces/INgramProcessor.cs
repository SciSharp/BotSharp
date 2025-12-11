namespace BotSharp.Plugin.FuzzySharp.Interfaces;

public interface INgramProcessor
{
    /// <summary>
    /// Process tokens and generate all possible n-gram match results
    /// </summary>
    /// <param name="tokens">List of tokens to process</param>
    /// <param name="vocabulary">Vocabulary (source -> vocabulary set)</param>
    /// <param name="synonymMapping">Synonym term Mapping</param>
    /// <param name="lookup">Lookup table (lowercase vocabulary -> (canonical form, source list))</param>
    /// <param name="maxNgram">Maximum n-gram length</param>
    /// <param name="cutoff">Minimum confidence threshold for fuzzy matching</param>
    /// <param name="topK">Maximum number of matches to return</param>
    /// <returns>List of flagged items</returns>
    List<FlaggedItem> ProcessNgrams(
        List<string> tokens,
        Dictionary<string, HashSet<string>> vocabulary,
        Dictionary<string, (string DataSource, string CanonicalForm)> synonymMapping,
        Dictionary<string, (string CanonicalForm, HashSet<string> Sources)> lookup,
        int maxNgram,
        double cutoff,
        int topK);
}
