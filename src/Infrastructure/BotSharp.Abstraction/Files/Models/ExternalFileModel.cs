namespace BotSharp.Abstraction.Files.Models;

public class ExternalFileModel : FileDataModel
{
    [JsonPropertyName("file_url")]
    public string? FileUrl { get; set; }
}
