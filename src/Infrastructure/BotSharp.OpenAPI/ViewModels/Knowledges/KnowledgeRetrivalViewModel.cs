using BotSharp.Abstraction.Knowledges.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeRetrivalViewModel
{
    [JsonPropertyName("data")]
    public IDictionary<string, string> Data { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("vector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Vector { get; set; }

    public static KnowledgeRetrivalViewModel From(KnowledgeRetrievalResult model)
    {
        return new KnowledgeRetrivalViewModel
        {
            Data = model.Data,
            Score = model.Score,
            Vector = model.Vector
        };
    }
}
