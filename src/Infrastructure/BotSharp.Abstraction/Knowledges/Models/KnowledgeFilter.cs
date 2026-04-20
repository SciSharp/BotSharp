using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeFilter : StringIdPagination
{
    [JsonPropertyName("db_provider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DbProvider { get; set; }

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }

    /// <summary>
    /// Filter group: each item contains a logical operator and a list of key-value pairs
    /// </summary>
    [JsonPropertyName("filter_groups")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<VectorFilterGroup>? FilterGroups { get; set; }

    /// <summary>
    /// Order by a specific field
    /// </summary>
    [JsonPropertyName("order_by")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VectorSort? OrderBy { get; set; }

    /// <summary>
    /// Included payload fields
    /// </summary>
    [JsonPropertyName("fields")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Fields { get; set; }
}
