namespace BotSharp.Abstraction.Knowledges.Options;

public class CollectionCreateOptions : KnowledgeOptionBase
{
    public int EmbeddingDimension { get; set; }
    public string LlmProvider { get; set; } = null!;
    public string LlmModel { get; set; } = null!;
}
