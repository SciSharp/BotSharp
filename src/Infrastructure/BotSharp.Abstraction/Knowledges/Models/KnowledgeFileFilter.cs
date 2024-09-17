namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeFileFilter : Pagination
{
    public IEnumerable<Guid>? FileIds { get; set; }
}
