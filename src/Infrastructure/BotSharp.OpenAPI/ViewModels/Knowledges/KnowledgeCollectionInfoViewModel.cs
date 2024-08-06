using BotSharp.Abstraction.Knowledges.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionInfoViewModel
{
    [JsonPropertyName("data_count")]
    public ulong DataCount { get; set; }

    [JsonPropertyName("vector_count")]
    public ulong VectorCount { get; set; }

    public static KnowledgeCollectionInfoViewModel ToViewModel(KnowledgeCollectionInfo info)
    {
        return new KnowledgeCollectionInfoViewModel
        {
            DataCount = info.DataCount,
            VectorCount = info.VectorCount
        };
    }
}
