namespace BotSharp.Abstraction.Knowledges.Options;

public class TaxonomyKnowledgeExecuteOptions : KnowledgeExecuteOptions
{
    public IEnumerable<string>? DataProviders { get; set; }
    public int? MaxNgram { get; set; }
}
