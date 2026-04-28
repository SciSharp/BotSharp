using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeUploadRequest
{
    /// <summary>
    /// Provider for knowledge file orchestrator
    /// </summary>
    [JsonPropertyName("orchestrator")]
    public string? Orchestrator { get; set; }

    [JsonPropertyName("files")]
    public IEnumerable<ExternalFileModel> Files { get; set; } = new List<ExternalFileModel>();

    [JsonPropertyName("options")]
    public KnowledgeFileHandleOptions? Options { get; set; }
}
