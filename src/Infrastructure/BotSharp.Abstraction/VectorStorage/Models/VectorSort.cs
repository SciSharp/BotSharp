namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorSort
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("order")]
    public string? Order { get; set; } = "desc";
}
