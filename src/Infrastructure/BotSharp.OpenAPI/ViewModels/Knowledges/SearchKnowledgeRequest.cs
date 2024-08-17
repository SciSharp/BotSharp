using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class SearchKnowledgeRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    #region Vector
    [JsonPropertyName("vector_params")]
    public VectorParam VectorParams { get; set; }
    #endregion

    #region Graph
    [JsonPropertyName("graph_params")]
    public GraphParam GraphParams { get; set; }
    #endregion
}

public class VectorParam
{
    [JsonPropertyName("collection")]
    public string Collection { get; set; }

    [JsonPropertyName("fields")]
    public IEnumerable<string>? Fields { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 5;

    [JsonPropertyName("confidence")]
    public float? Confidence { get; set; } = 0.5f;

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }
} 

public class GraphParam
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
}