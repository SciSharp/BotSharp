using BotSharp.Abstraction.VectorStorage.Options;

namespace BotSharp.Abstraction.Knowledges.Options;

public class KnowledgeIndexOptions : KnowledgeOptionBase
{
    public IEnumerable<CollectionIndexOptions> Items { get; set; } = [];
}
