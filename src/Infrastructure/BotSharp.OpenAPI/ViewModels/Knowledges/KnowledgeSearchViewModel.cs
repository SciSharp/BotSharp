using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeSearchViewModel
{
    [JsonPropertyName("vector_result")]
    public IEnumerable<VectorKnowledgeViewModel>? VectorResult { get; set; }

    [JsonPropertyName("graph_result")]
    public GraphKnowledgeViewModel? GraphResult { get; set; }
}
