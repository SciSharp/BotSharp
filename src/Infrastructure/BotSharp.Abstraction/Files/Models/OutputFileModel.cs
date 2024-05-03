namespace BotSharp.Abstraction.Files.Models;

public class OutputFileModel
{
    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("file_type")]
    public string FileType { get; set; }
}
