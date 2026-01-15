using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Graph.Options;

namespace BotSharp.Abstraction.Knowledges;

public interface IGraphKnowledgeService
{
    Task<GraphSearchResult> SearchAsync(string query, GraphSearchOptions? options = null);
}
