using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class SearchVectorKnowledgeRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public IEnumerable<string>? Fields { get; set; }

    [JsonPropertyName("filters")]
    public IEnumerable<KeyValue>? Filters { get; set; }

    [JsonPropertyName("filter_operator")]
    public string FilterOperator { get; set; } = "and";

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 5;

    [JsonPropertyName("confidence")]
    public float? Confidence { get; set; } = 0.5f;

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }
}
