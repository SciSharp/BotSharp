namespace BotSharp.Abstraction.Knowledges.Options;

public class TaxonomyKnowledgeSearchOptions : KnowledgeSearchOptions
{
    public IEnumerable<string>? DataProviders { get; set; }
    public int? MaxNgram { get; set; }
}
