
namespace BotSharp.Abstraction.Files.Models;

public class BotSharpFile : FileInfo
{
    /// <summary>
    /// File data => format: "data:image/png;base64,aaaaaaaa"
    /// </summary>
    [JsonPropertyName("file_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileData { get; set; } = string.Empty;
}
