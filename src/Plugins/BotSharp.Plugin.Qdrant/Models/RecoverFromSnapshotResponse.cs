using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Qdrant.Models;

public class RecoverFromSnapshotResponse
{
    [JsonPropertyName("time")]
    public decimal Time { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("result")]
    public bool Result { get; set; }
}
