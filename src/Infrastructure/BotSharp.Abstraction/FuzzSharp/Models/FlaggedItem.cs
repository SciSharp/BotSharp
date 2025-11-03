
namespace BotSharp.Abstraction.FuzzSharp.Models
{
    public class FlaggedItem
    {
        /// <summary>
        /// Token index in the original text
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Original token text
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Domain types where this token was found (e.g., ['client_Profile.Name', 'data_ServiceType.Name'])
        /// </summary>
        [JsonPropertyName("domain_types")]
        public List<string> DomainTypes { get; set; } = new();

        /// <summary>
        /// Type of match: 'domain_term_mapping' (business abbreviations, confidence=1.0) | 
        /// 'exact_match' (vocabulary matches, confidence=1.0) | 
        /// 'typo_correction' (spelling corrections, confidence less than 1.0)
        /// </summary>
        [JsonPropertyName("match_type")]
        public string MatchType { get; set; } = string.Empty;

        /// <summary>
        /// Canonical form or suggested correction
        /// </summary>
        [JsonPropertyName("canonical_form")]
        public string CanonicalForm { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score (0.0-1.0, where 1.0 is exact match)
        /// </summary>
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        /// <summary>
        /// N-gram length (number of tokens in this match). Internal field, not included in JSON output.
        /// </summary>
        [JsonIgnore]
        public int NgramLength { get; set; }
    }
}
