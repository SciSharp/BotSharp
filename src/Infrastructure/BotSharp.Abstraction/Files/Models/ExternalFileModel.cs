namespace BotSharp.Abstraction.Files.Models;

public class ExternalFileModel : FileDataModel
{
    [JsonPropertyName("file_url")]
    public string? FileUrl { get; set; }

    /// <summary>
    /// File data => format: "data:image/png;base64,aaaaaaaa"
    /// </summary>
    [JsonPropertyName("file_data")]
    public new string? FileData { get; set; }
}
