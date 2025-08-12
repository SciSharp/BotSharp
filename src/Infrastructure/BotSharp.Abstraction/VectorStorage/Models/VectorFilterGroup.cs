namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorFilterGroup
{
    [JsonPropertyName("filters")]
    public IEnumerable<KeyValue>? Filters { get; set; }

    [JsonPropertyName("filter_operator")]
    public string FilterOperator { get; set; } = "or";
}
