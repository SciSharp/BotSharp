
namespace BotSharp.Abstraction.Files.Models;

public class BotSharpFile
{
    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("file_data")]
    public string FileData { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("file_size")]
    public int FileSize { get; set; }
}
