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
    Task<CypherQueryResponse> CypherQueryAsync([Query] string graphId, CypherQueryRequest request);

    [Post("/graph/{graphId}/node")]
    Task<Node> CreateNodeAsync(string graphId, [Body] NodeCreationModel node);

    [Get("/graph/{graphId}/node/{nodeId}")]
    Task<Node> GetNodeAsync(string graphId, string nodeId);

    [Put("/graph/{graphId}/node/{nodeId}")]
    Task<Node> UpdateNodeAsync(string graphId, string nodeId, [Body] NodeUpdateModel node);

    [Put("/graph/{graphId}/node/{nodeId}/merge")]
    Task<Node> MergeNodeAsync(string graphId, string nodeId, [Body] NodeUpdateModel node);

    [Delete("/graph/{graphId}/node/{nodeId}")]
    Task<IActionResult> DeleteNodeAsync(string graphId, string nodeId);
}
