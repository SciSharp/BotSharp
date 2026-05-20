namespace BotSharp.Abstraction.Knowledges.Filters;

public class KnowledgeFileFilter : Pagination
{
    public string? DbProvider { get; set; }
    public IEnumerable<Guid>? FileIds { get; set; }
    public IEnumerable<string>? FileNames { get; set; }
    public IEnumerable<string>? ContentTypes { get; set; }
    public IEnumerable<string>? FileSources { get; set; }

    public KnowledgeFileFilter()
    {
        
    }
}
