using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorKnowledgeViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("data")]
    public IDictionary<string, VectorPayloadValue> Data { get; set; }

    [JsonPropertyName("score")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Score { get; set; }

    [JsonPropertyName("vector_dimension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? VectorDimension { get; set; }


    public static VectorKnowledgeViewModel From(VectorSearchResult result)
    {
        return new VectorKnowledgeViewModel
        {
            Id = result.Id,
            Data = result.Data,
            Score = result.Score,
            VectorDimension = result.Vector?.Length
        };
    }

    public static VectorKnowledgeViewModel From(VectorCollectionData data)
    {
        return new VectorKnowledgeViewModel
        {
            Id = data.Id,
            Data = data.Data,
            VectorDimension = data.Vector?.Length
        };
    }
}
