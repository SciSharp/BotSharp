using BotSharp.Abstraction.Graph.Models;

namespace BotSharp.Abstraction.Graph;

public interface IGraphDb
{
    public string Provider { get; }

    Task<GraphSearchData> Search(string query, GraphSearchOptions options);
}
