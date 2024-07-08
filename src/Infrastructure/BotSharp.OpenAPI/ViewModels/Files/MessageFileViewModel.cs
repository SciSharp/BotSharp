using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Files;

public class MessageFileViewModel
{
    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("file_type")]
    public string FileType { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("file_source")]
    public string FileSource { get; set; }

    public MessageFileViewModel()
    {
        
    }

    public static MessageFileViewModel Transform(MessageFileModel model)
    {
        return new MessageFileViewModel
        {
            FileUrl = model.FileUrl,
            FileName = model.FileName,
            FileType = model.FileType,
            ContentType = model.ContentType,
            FileSource = model.FileSource
        };
    }
}
