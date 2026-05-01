namespace BotSharp.Abstraction.Knowledges.Options;

public class TaxonomyKnowledgeSearchOptions : KnowledgeExecuteOptions
{
    public IEnumerable<string>? DataProviders { get; set; }
    public int? MaxNgram { get; set; }
}
