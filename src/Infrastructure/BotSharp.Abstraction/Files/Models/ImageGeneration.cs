namespace BotSharp.Abstraction.Files.Models;

public class ImageGeneration
{
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("image_data")]
    public string? ImageData { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
