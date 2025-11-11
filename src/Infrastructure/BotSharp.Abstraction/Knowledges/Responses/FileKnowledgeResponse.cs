namespace BotSharp.Abstraction.Knowledges.Responses;

public class FileKnowledgeResponse : ResponseBase
{
    public IEnumerable<FileKnowledgeModel> Knowledges { get; set; } = [];
}
