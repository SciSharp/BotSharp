using System.Text.Json.Serialization;

namespace BotSharp.Plugin.CodeDriver.Models;

public class SaveSourceCodeArgs
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("source_code")]
    public string SourceCode { get; set; } = string.Empty;
}
