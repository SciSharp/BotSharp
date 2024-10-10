using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeCreateRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("data_source")]
    public string DataSource { get; set; } = VectorDataSource.Api;

    [JsonPropertyName("payload")]
    public Dictionary<string, object>? Payload { get; set; }
}
