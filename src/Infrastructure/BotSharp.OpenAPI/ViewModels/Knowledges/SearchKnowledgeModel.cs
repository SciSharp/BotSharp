using BotSharp.Abstraction.Knowledges.Enums;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class SearchKnowledgeModel
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public IEnumerable<string>? Fields { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 5;

    [JsonPropertyName("confidence")]
    public float? Confidence { get; set; } = 0.5f;

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }
}
