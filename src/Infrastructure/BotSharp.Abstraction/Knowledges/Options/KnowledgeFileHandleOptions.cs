namespace BotSharp.Abstraction.Knowledges.Options;

public class KnowledgeFileHandleOptions : FileKnowledgeHandleOptions
{
    public string? DbProvider { get; set; }
    public string? Processor { get; set; }
}
