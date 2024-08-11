namespace BotSharp.Abstraction.Files.Models;

public class FileBase
{
    /// <summary>
    /// External file url
    /// </summary>
    [JsonPropertyName("file_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Internal file storage url
    /// </summary>
    [JsonPropertyName("file_storage_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileStorageUrl { get; set; } = string.Empty;

    /// <summary>
    /// File name without extension
    /// </summary>
    [JsonPropertyName("file_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileName { get; set; } = string.Empty;

    /// <summary>
    /// File data, e.g., "data:image/png;base64,aaaaaaaa"
    /// </summary>
    [JsonPropertyName("file_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileData { get; set; } = string.Empty;

    /// <summary>
    /// File content type
    /// </summary>
    [JsonPropertyName("content_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File extension without dot
    /// </summary>
    [JsonPropertyName("file_extension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileExtension { get; set; } = string.Empty;
}
