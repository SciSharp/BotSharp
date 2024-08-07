using BotSharp.Abstraction.Knowledges.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionDataViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; }

    [JsonPropertyName("vector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Vector { get; set; }

    public static KnowledgeCollectionDataViewModel ToViewModel(KnowledgeCollectionData data)
    {
        return new KnowledgeCollectionDataViewModel
        {
            Id = data.Id,
            Question = data.Question,
            Answer = data.Answer,
            Vector = data.Vector
        };
    }
}
