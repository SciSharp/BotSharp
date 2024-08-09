using BotSharp.Abstraction.Knowledges.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionDataViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("question")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Question { get; set; }

    [JsonPropertyName("answer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Answer { get; set; }

    [JsonPropertyName("vector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Vector { get; set; }

    public static KnowledgeCollectionDataViewModel From(KnowledgeCollectionData data)
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
