using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeUploadRequest
{
    [JsonPropertyName("files")]
    public IEnumerable<ExternalFileModel> Files { get; set; } = new List<ExternalFileModel>();

    [JsonPropertyName("chunk_option")]
    public ChunkOption? ChunkOption { get; set; }
}
