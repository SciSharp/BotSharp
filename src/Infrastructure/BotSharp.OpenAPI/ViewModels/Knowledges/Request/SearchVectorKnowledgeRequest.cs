using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class SearchKnowledgeRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public IEnumerable<string>? Fields { get; set; }

    [JsonPropertyName("filter_groups")]
    public IEnumerable<VectorFilterGroup>? FilterGroups { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 5;

    [JsonPropertyName("confidence")]
    public float? Confidence { get; set; } = 0.5f;

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }

    [JsonPropertyName("search_param")]
    public Dictionary<string, string>? SearchParam { get; set; }

    [JsonPropertyName("data_providers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? DataProviders { get; set; }

    [JsonPropertyName("db_provider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DbProvider { get; set; }

    [JsonPropertyName("knowledge_type")]
    public string KnowledgeType { get; set; } = null!;
}
