namespace BotSharp.Abstraction.Files.Models;

public class FileInformation
{
    /// <summary>
    /// External file url for display
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
    /// File content type
    /// </summary>
    [JsonPropertyName("content_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File name without extension
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File extension without dot
    /// </summary>
    [JsonPropertyName("file_extension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// External file url for download
    /// </summary>
    [JsonPropertyName("file_download_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileDownloadUrl { get; set; } = string.Empty;
}
