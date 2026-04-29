using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeFilter : StringIdPagination
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DbProvider { get; set; }

    public bool WithVector { get; set; }

    /// <summary>
    /// Filter group: each item contains a logical operator and a list of key-value pairs
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<VectorFilterGroup>? FilterGroups { get; set; }

    /// <summary>
    /// Order by a specific field
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VectorSort? OrderBy { get; set; }

    /// <summary>
    /// Included payload fields
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Fields { get; set; }
}
