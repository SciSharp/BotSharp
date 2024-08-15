namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorFilter : StringIdPagination
{
    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }
}
