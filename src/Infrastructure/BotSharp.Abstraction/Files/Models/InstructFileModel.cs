namespace BotSharp.Abstraction.Files.Models;

public class InstructFileModel : FileBase
{
    /// <summary>
    /// File extension without dot
    /// </summary>
    [JsonPropertyName("file_extension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// File url
    /// </summary>
    [JsonPropertyName("file_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// File MIME type
    /// </summary>
    [JsonPropertyName("content_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentType { get; set; }
}
