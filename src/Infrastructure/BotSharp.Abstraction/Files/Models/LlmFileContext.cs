namespace BotSharp.Abstraction.Files.Models;

public class LlmFileContext
{
    [JsonPropertyName("user_request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserRequest { get; set; }

    [JsonPropertyName("file_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileTypes { get; set; }

    [JsonPropertyName("image_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageDescription { get; set; }
}
