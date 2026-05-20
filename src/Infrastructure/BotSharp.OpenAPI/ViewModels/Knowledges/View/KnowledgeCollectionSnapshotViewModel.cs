using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionSnapshotViewModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; }

    [JsonPropertyName("check_sum")]
    public string? CheckSum { get; set; }

    public static KnowledgeCollectionSnapshotViewModel? From(VectorCollectionSnapshot? model)
    {
        if (model == null)
        {
            return null;
        }

        return new KnowledgeCollectionSnapshotViewModel
        {
            Name = model.Name,
            Size = model.Size,
            CreatedTime = model.CreatedTime,
            CheckSum = model.CheckSum
        };
    }

    public static KnowledgeCollectionSnapshotViewModel? From(KnowledgeCollectionSnapshot? model)
    {
        if (model == null)
        {
            return null;
        }

        return new KnowledgeCollectionSnapshotViewModel
        {
            Name = model.Name,
            Size = model.Size,
            CreatedTime = model.CreatedTime,
            CheckSum = model.CheckSum
        };
    }
}
