namespace BotSharp.Abstraction.Files.Models;

public class FileDataModel : FileBase
{
    /// <summary>
    /// File name with extension
    /// </summary>
    [JsonPropertyName("file_name")]
    public new string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File data => format: "data:image/png;base64,aaaaaaaa"
    /// </summary>
    [JsonPropertyName("file_data")]
    public new string FileData { get; set; } = string.Empty;
}
