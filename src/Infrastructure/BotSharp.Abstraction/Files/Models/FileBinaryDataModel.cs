namespace BotSharp.Abstraction.Files.Models;

public class FileBinaryDataModel
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public BinaryData FileBinaryData { get; set; }
}
