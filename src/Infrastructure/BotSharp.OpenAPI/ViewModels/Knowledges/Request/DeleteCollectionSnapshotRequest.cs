using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class DeleteCollectionSnapshotRequest
{
    [JsonPropertyName("knowledge_type")]
    public string KnowledgeType { get; set; } = null!;

    [JsonPropertyName("snapshot_name")]
    public string SnapshotName { get; set; } = default!;
}
