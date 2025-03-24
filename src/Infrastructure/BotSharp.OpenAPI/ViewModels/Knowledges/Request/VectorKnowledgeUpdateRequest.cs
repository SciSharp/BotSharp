using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeUpdateRequest : VectorKnowledgeCreateRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
