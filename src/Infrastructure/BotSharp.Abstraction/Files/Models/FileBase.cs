namespace BotSharp.Abstraction.Files.Models;

public class FileBase
{
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
}
