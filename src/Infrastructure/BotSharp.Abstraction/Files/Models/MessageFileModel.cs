namespace BotSharp.Abstraction.Files.Models;

public class MessageFileModel : FileInformation
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }

    [JsonPropertyName("file_source")]
    public string FileSource { get; set; } = Enums.FileSource.User;

    [JsonPropertyName("file_index")]
    public string FileIndex { get; set; } = string.Empty;

    public MessageFileModel()
    {
        
    }

    public override string ToString()
    {
        return $"File name: {FileName}, File extension: {FileExtension}, Content type: {ContentType}, Source: {FileSource}";
    }
}
