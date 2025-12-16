using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Models.Image;

public class ImageGenerationResponse
{
    [JsonPropertyName("output_format")]
    public string? OutputFormat { get; set; }

    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }
}
