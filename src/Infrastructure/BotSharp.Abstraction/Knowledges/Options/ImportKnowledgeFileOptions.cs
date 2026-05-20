using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Options;

public class ImportKnowledgeFileOptions : KnowledgeOptionBase
{
    public DocMetaRefData? FileRefData { get; set; }
    public Dictionary<string, VectorPayloadValue>? Payload { get; set; }
}
