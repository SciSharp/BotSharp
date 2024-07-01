using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Embeddings;

public class EmbeddingInputModel : MessageConfig
{
    [JsonPropertyName("texts")]
    public IEnumerable<string> Texts { get; set; } = new List<string>();

    [JsonPropertyName("dimension")]
    public int Dimension { get; set; } = 1536;
}
