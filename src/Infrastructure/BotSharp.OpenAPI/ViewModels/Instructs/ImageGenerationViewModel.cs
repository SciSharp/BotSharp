using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class ImageGenerationViewModel
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("images")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ImageViewModel> Images { get; set; } = new List<ImageViewModel>();

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}

public class ImageViewModel
{
    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("image_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageData { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    public static ImageViewModel ToViewModel(ImageGeneration image)
    {
        return new ImageViewModel
        {
            ImageUrl = image.ImageUrl,
            ImageData = image.ImageData,
            Description = image.Description
        };
    }
}