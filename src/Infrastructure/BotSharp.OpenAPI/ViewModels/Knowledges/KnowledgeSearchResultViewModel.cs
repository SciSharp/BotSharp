using BotSharp.Abstraction.Knowledges.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeSearchResultViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("data")]
    public IDictionary<string, string> Data { get; set; }

    [JsonPropertyName("score")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Score { get; set; }

    [JsonPropertyName("vector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Vector { get; set; }


    public static KnowledgeSearchResultViewModel From(KnowledgeSearchResult result)
    {
        return new KnowledgeSearchResultViewModel
        {
            Id = result.Id,
            Data = result.Data,
            Score = result.Score,
            Vector = result.Vector
        };
    }
}
