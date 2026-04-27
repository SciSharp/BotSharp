using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Graph.Options;
using BotSharp.Abstraction.Graph.Requests;

namespace BotSharp.Abstraction.Graph;

public interface IGraphDb
{
    public string Provider { get; }

    Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryExecuteOptions? options = null)
        => throw new NotImplementedException();

    #region Node
    Task<GraphNodeModel> GetNodeAsync(string graphId, string nodeId)
        => throw new NotImplementedException();

    Task<GraphNodeModel> CreateNodeAsync(string graphId, GraphNodeCreationRequest request)
        => throw new NotImplementedException();

    Task<GraphNodeModel> MergeNodeAsync(string graphId, string nodeId, GraphNodeUpdateRequest request)
        => throw new NotImplementedException();

    Task<bool> DeleteNodeAsync(string graphId, string nodeId)
        => throw new NotImplementedException();
    #endregion

    #region Edge
    Task<GraphEdgeModel> GetEdgeAsync(string graphId, string edgeId)
        => throw new NotImplementedException();

    Task<GraphEdgeModel> CreateEdgeAsync(string graphId, GraphEdgeCreationRequest request)
        => throw new NotImplementedException();

    Task<GraphEdgeModel> UpdateEdgeAsync(string graphId, string edgeId, GraphEdgeUpdateRequest request)
        => throw new NotImplementedException();

    Task<bool> DeleteEdgeAsync(string graphId, string edgeId)
        => throw new NotImplementedException();
    #endregion
}
