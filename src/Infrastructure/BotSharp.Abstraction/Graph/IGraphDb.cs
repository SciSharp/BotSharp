using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Graph.Options;

namespace BotSharp.Abstraction.Graph;

public interface IGraphDb
{
    public string Provider { get; }

    Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryExecuteOptions? options = null)
        => throw new NotImplementedException();
}
