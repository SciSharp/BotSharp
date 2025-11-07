using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Models;

public class FileKnowledgeModel
{
    public IEnumerable<string> Contents { get; set; } = [];
    public IDictionary<string, VectorPayloadValue>? Payload { get; set; }
}


public class FileKnowledgeWrapper
{
    public Guid FileId { get; set; }
    public string? FileSource { get; set; }
    public FileBinaryDataModel FileData { get; set; }
    public IEnumerable<FileKnowledgeModel> FileKnowledges { get; set; } = [];
}