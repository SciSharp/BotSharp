namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorFilter : StringIdPagination
{
    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }

    /// <summary>
    /// For keyword search
    /// </summary>
    [JsonPropertyName("filters")]
    public IEnumerable<KeyValue>? Filters { get; set; }

    /// <summary>
    /// Filter operator
    /// </summary>
    [JsonPropertyName("filter_operator")]
    public string FilterOperator { get; set; } = "or";

    /// <summary>
    /// Included payload fields
    /// </summary>
    [JsonPropertyName("fields")]
    public IEnumerable<string>? Fields { get; set; }
}