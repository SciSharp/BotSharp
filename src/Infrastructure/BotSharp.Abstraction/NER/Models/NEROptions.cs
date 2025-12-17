namespace BotSharp.Abstraction.NER.Models;

public class NEROptions
{
    /// <summary>
    /// Token data providers
    /// </summary>
    [JsonPropertyName("data_providers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? DataProviders { get; set; }

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
