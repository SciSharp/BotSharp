using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Graph.Options;

namespace BotSharp.Abstraction.Graph;

public interface IGraphKnowledgeService
{
    Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryOptions? options = null);
}
