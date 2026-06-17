namespace BotSharp.Abstraction.Entity.Models;

public class EntityAnalysisOptions
{
    /// <summary>
    /// Token data providers
    /// </summary>
    [JsonPropertyName("data_providers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? DataProviders { get; set; }

    /// <summary>
    /// Free-form parameters forwarded to <see cref="IEntityDataLoader"/> implementations.
    /// Each loader documents the keys it recognizes (e.g. "graphId" for graph-backed loaders).
    /// </summary>
    [JsonPropertyName("loader_parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string>? LoaderParameters { get; set; }

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
