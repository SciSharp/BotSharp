using BotSharp.Abstraction.Knowledges.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionDataViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

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
            Text = data.Text,
            Answer = data.Answer,
            Vector = data.Vector
        };
    }
}
