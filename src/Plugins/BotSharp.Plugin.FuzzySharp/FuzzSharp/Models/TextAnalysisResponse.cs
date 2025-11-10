using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.FuzzSharp.Models;

public class TextAnalysisResponse
{
    /// <summary>
    /// Original text
    /// </summary>
    [JsonPropertyName("original")]
    public string Original { get; set; } = string.Empty;

    /// <summary>
    /// Tokenized text (only included if include_tokens=true)
    /// </summary>
    [JsonPropertyName("tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tokens { get; set; }

    /// <summary>
    /// Flagged items (filter by 'match_type' field: 'domain_term_mapping', 'exact_match', or 'typo_correction')
    /// </summary>
    [JsonPropertyName("flagged")]
    public List<FlaggedItem> Flagged { get; set; } = new();

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    [JsonPropertyName("processing_time_ms")]
    public double ProcessingTimeMs { get; set; }
}