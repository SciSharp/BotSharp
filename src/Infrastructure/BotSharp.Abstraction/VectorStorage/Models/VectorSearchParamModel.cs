namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorSearchParamModel
{
    [JsonPropertyName("exact_search")]
    public bool? ExactSearch { get; set; }
}
