using Refit;

namespace BotSharp.Plugin.Membase.Services;

/// <summary>
/// Membase REST API interface
/// https://membase.dev/graph-api-reference
/// </summary>
public interface IMembaseApi
{
    [Get("/graph/{graphId}")]
    Task<GraphInfo> GetGraphInfoAsync(string graphId);

    [Post("/cypher/execute")]
    Task<CypherQueryResponse> CypherQueryAsync([Query] string graphId, [Body] CypherQueryRequest request);

    #region Node
    [Get("/graph/{graphId}/node/{nodeId}")]
    Task<Node> GetNodeAsync(string graphId, string nodeId);

    [Post("/graph/{graphId}/node")]
    Task<Node> CreateNodeAsync(string graphId, [Body] NodeCreationModel node);

    [Put("/graph/{graphId}/node/{nodeId}")]
    Task<Node> UpdateNodeAsync(string graphId, string nodeId, [Body] NodeUpdateModel node);

    [Put("/graph/{graphId}/node/{nodeId}/merge")]
    Task<Node> MergeNodeAsync(string graphId, string nodeId, [Body] NodeUpdateModel node);

    [Delete("/graph/{graphId}/node/{nodeId}")]
    Task<NodeDeleteResponse?> DeleteNodeAsync(string graphId, string nodeId);
    #endregion

    #region Edge
    [Get("/graph/{graphId}/edge/{edgeId}")]
    Task<Edge> GetEdgeAsync(string graphId, string edgeId);

    [Post("/graph/{graphId}/edge")]
    Task<Edge> CreateEdgeAsync(string graphId, [Body] EdgeCreationModel edge);

    [Put("/graph/{graphId}/edge/{edgeId}")]
    Task<Edge> UpdateEdgeAsync(string graphId, string edgeId, [Body] EdgeUpdateModel edge);

    [Delete("/graph/{graphId}/edge/{edgeId}")]
    Task<EdgeDeleteResponse> DeleteEdgeAsync(string graphId, string edgeId);
    #endregion
}
