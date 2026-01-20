using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Graph.Options;
using BotSharp.Abstraction.Graph.Requests;
using BotSharp.Abstraction.Graph.Responses;

namespace BotSharp.Abstraction.Graph;

public interface IGraphDb
{
    public string Provider { get; }

    Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryExecuteOptions? options = null)
        => throw new NotImplementedException();

    #region Node
    Task<GraphNode?> GetNodeAsync(string graphId, string nodeId)
        => throw new NotImplementedException();
    Task<GraphNode> CreateNodeAsync(string graphId, GraphNodeCreationModel node)
        => throw new NotImplementedException();
    Task<GraphNode> UpdateNodeAsync(string graphId, string nodeId, GraphNodeUpdateModel node)
        => throw new NotImplementedException();
    Task<GraphNode> UpsertNodeAsync(string graphId, string nodeId, GraphNodeUpdateModel node)
        => throw new NotImplementedException();
    Task<GraphNodeDeleteResponse?> DeleteNodeAsync(string graphId, string nodeId)
        => throw new NotImplementedException();
    #endregion
}
