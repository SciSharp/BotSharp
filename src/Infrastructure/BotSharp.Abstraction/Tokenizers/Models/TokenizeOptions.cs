namespace BotSharp.Abstraction.Tokenizers.Models;

public class TokenizeOptions
{
    /// <summary>
    /// Maximum n-gram size
    /// </summary>
    [JsonPropertyName("max_ngram")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxNgram { get; set; }

    /// <summary>
    /// Cutoff score: from 0 to 1
    /// </summary>
    [JsonPropertyName("cutoff")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Cutoff { get; set; }

    /// <summary>
    /// Top k
    /// </summary>
    [JsonPropertyName("top_k")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TopK { get; set; }
}
