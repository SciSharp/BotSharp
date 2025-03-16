using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class DeleteVectorCollectionSnapshotRequest
{
    [JsonPropertyName("snapshot_name")]
    public string SnapshotName { get; set; } = default!;
}
