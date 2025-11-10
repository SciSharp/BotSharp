using BotSharp.Abstraction.FuzzSharp.Models;

namespace BotSharp.Abstraction.FuzzSharp
{
    public interface INgramProcessor
    {
        /// <summary>
        /// Process tokens and generate all possible n-gram match results
        /// </summary>
        /// <param name="tokens">List of tokens to process</param>
        /// <param name="vocabulary">Vocabulary (domain type -> vocabulary set)</param>
        /// <param name="domainTermMapping">Domain term mapping</param>
        /// <param name="lookup">Lookup table (lowercase vocabulary -> (canonical form, domain type list))</param>
        /// <param name="maxNgram">Maximum n-gram length</param>
        /// <param name="cutoff">Minimum confidence threshold for fuzzy matching</param>
        /// <param name="topK">Maximum number of matches to return</param>
        /// <returns>List of flagged items</returns>
        List<FlaggedItem> ProcessNgrams(
            List<string> tokens,
            Dictionary<string, HashSet<string>> vocabulary,
            Dictionary<string, (string DbPath, string CanonicalForm)> domainTermMapping,
            Dictionary<string, (string CanonicalForm, List<string> DomainTypes)> lookup,
            int maxNgram,
            double cutoff,
            int topK);
    }
}
