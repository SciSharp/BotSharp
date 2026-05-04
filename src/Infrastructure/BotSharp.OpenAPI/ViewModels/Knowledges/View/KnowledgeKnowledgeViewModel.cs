using BotSharp.Abstraction.VectorStorage.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeKnowledgeViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("payload")]
    public IDictionary<string, VectorPayloadValue> Payload { get; set; }

    [JsonPropertyName("data")]
    public IDictionary<string, object> Data { get; set; }

    [JsonPropertyName("score")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Score { get; set; }

    [JsonPropertyName("vector_dimension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? VectorDimension { get; set; }

    public static KnowledgeKnowledgeViewModel From(KnowledgeExecuteResult result)
    {
        return new KnowledgeKnowledgeViewModel
        {
            Id = result.Id,
            Data = result.Data,
            Payload = result.Payload,
            Score = result.Score,
            VectorDimension = result.Vector?.Length
        };
    }

    public static KnowledgeKnowledgeViewModel From(KnowledgeCollectionData data)
    {
        return new KnowledgeKnowledgeViewModel
        {
            Id = data.Id,
            Data = data.Data,
            Payload = data.Payload,
            VectorDimension = data.Vector?.Length
        };
    }
}
