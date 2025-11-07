using BotSharp.Abstraction.Knowledges.Options;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeUploadRequest
{
    [JsonPropertyName("files")]
    public IEnumerable<ExternalFileModel> Files { get; set; } = new List<ExternalFileModel>();

    [JsonPropertyName("options")]
    public KnowledgeDocOptions? Options { get; set; }
}
