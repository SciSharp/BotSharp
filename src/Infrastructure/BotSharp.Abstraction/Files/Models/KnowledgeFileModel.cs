namespace BotSharp.Abstraction.Files.Models;

public class KnowledgeFileModel
{
    public Guid FileId { get; set; }
    public string FileName { get; set; }
    public string FileExtension { get; set; }
    public string ContentType { get; set; }
    public string FileUrl { get; set; }
}
