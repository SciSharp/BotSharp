
namespace BotSharp.Plugin.FuzzySharp.Constants
{
    public static class MatchReason
    {
        /// <summary>
        /// Token matched a domain term mapping (e.g., HVAC -> Air Conditioning/Heating)
        /// </summary>
        public const string DomainTermMapping = "domain_term_mapping";

        /// <summary>
        /// Token exactly matched a vocabulary entry
        /// </summary>
        public const string ExactMatch = "exact_match";

        /// <summary>
        /// Token was flagged as a potential typo and a correction was suggested
        /// </summary>
        public const string TypoCorrection = "typo_correction";
    }
}
