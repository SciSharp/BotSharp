using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeSearchResult
{
    public IEnumerable<VectorSearchResult> VectorResult { get; set; }
    public GraphSearchResult GraphResult { get; set; }
}
