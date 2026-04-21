using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeUpdateRequest : KnowledgeCreateRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
