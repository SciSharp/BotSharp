namespace BotSharp.Abstraction.Knowledges.Responses;

public class FileKnowledgeResponse
{
    public IEnumerable<FileKnowledgeModel> Knowledges { get; set; } = [];
}
