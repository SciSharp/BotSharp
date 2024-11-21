using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Files;

public class MessageFileViewModel
{
    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("file_extension")]
    public string FileExtension { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("file_source")]
    public string FileSource { get; set; }

    [JsonPropertyName("file_download_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileDownloadUrl { get; set; }

    public MessageFileViewModel()
    {
        
    }

    public static MessageFileViewModel Transform(MessageFileModel model)
    {
        return new MessageFileViewModel
        {
            FileUrl = model.FileUrl,
            FileName = model.FileName,
            FileExtension = model.FileExtension,
            ContentType = model.ContentType,
            FileSource = model.FileSource,
            FileDownloadUrl = model.FileDownloadUrl
        };
    }
}
