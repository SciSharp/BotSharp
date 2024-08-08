namespace BotSharp.Abstraction.Files.Models;

public class MessageFileModel : FileBase
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }

    [JsonPropertyName("file_source")]
    public string FileSource { get; set; } = FileSourceType.User;

    public MessageFileModel()
    {
        
    }

    public override string ToString()
    {
        return $"File name: {FileName}, File type: {FileType}, Content type: {ContentType}, Source: {FileSource}";
    }
}
