using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Graph.Options;
using BotSharp.Abstraction.Graph.Requests;
using BotSharp.Abstraction.Graph.Responses;

namespace BotSharp.Abstraction.Graph;

public interface IGraphKnowledgeService
{
    Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryOptions? options = null);

    #region Node
    Task<GraphNode?> GetNodeAsync(string graphId, string nodeId, GraphNodeOptions? options = null);
    Task<GraphNode> CreateNodeAsync(string graphId, GraphNodeCreationModel node, GraphNodeOptions? options = null);
    Task<GraphNode> UpdateNodeAsync(string graphId, string nodeId, GraphNodeUpdateModel node, GraphNodeOptions? options = null);
    Task<GraphNode> UpsertNodeAsync(string graphId, string nodeId, GraphNodeUpdateModel node, GraphNodeOptions? options = null);
    Task<GraphNodeDeleteResponse?> DeleteNodeAsync(string graphId, string nodeId, GraphNodeOptions? options = null);
    #endregion
}
