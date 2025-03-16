using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("data")]
    public IDictionary<string, object> Data { get; set; }

    [JsonPropertyName("score")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Score { get; set; }

    [JsonPropertyName("vector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Vector { get; set; }


    public static VectorKnowledgeViewModel From(VectorSearchResult result)
    {
        return new VectorKnowledgeViewModel
        {
            Id = result.Id,
            Data = result.Data,
            Score = result.Score,
            Vector = result.Vector
        };
    }
}
