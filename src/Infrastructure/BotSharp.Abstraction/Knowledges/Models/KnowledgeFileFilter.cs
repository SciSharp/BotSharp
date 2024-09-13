namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeFileFilter : Pagination
{
    public IEnumerable<string>? FileIds { get; set; }
}
