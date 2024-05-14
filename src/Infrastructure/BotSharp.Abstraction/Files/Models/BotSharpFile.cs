
namespace BotSharp.Abstraction.Files.Models;

public class BotSharpFile
{
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_data")]
    public string FileData { get; set; } = string.Empty;

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; } = string.Empty;
}
