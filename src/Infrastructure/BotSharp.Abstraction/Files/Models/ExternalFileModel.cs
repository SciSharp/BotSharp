using BotSharp.Abstraction.Knowledges.Enums;

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

    /// <summary>
    /// The file source, e.g., api, user upload, external web, etc.
    /// </summary>
    [JsonPropertyName("file_source")]
    public string FileSource { get; set; } = KnowledgeDocSource.Api;
}
