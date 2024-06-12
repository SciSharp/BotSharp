namespace BotSharp.Abstraction.Files.Models;

public class MessageFileModel
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; }

    [JsonPropertyName("file_storage_url")]
    public string FileStorageUrl { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("file_type")]
    public string FileType { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("file_source")]
    public string FileSource { get; set; } = FileSourceType.User;

    public MessageFileModel()
    {
        
    }

    public override string ToString()
    {
        return $"File name: {FileName}, File type: {FileType}, Content type: {ContentType}";
    }
}
