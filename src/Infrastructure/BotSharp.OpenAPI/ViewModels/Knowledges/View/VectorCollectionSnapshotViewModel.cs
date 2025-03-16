using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorCollectionSnapshotViewModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; }

    [JsonPropertyName("check_sum")]
    public string? CheckSum { get; set; }

    public static VectorCollectionSnapshotViewModel? From(VectorCollectionSnapshot? model)
    {
        if (model == null)
        {
            return null;
        }

        return new VectorCollectionSnapshotViewModel
        {
            Name = model.Name,
            Size = model.Size,
            CreatedTime = model.CreatedTime,
            CheckSum = model.CheckSum
        };
    }
}
