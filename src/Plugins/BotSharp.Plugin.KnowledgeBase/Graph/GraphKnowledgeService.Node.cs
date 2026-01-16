using BotSharp.Abstraction.Graph.Options;
using BotSharp.Abstraction.Graph.Requests;
using BotSharp.Abstraction.Graph.Responses;

namespace BotSharp.Plugin.KnowledgeBase.Graph;

public partial class GraphKnowledgeService
{
    public async Task<GraphNode?> GetNodeAsync(string graphId, string nodeId, GraphNodeOptions? options = null)
    {
        var db = GetGraphDb(options?.Provider);
        var result = await db.GetNodeAsync(graphId, nodeId);
        return result;
    }

    public async Task<GraphNode> CreateNodeAsync(string graphId, GraphNodeCreationModel node, GraphNodeOptions? options = null)
    {
        var db = GetGraphDb(options?.Provider);
        var result = await db.CreateNodeAsync(graphId, node);
        return result;
    }

    public async Task<GraphNode> UpdateNodeAsync(string graphId, string nodeId, GraphNodeUpdateModel node, GraphNodeOptions? options = null)
    {
        var db = GetGraphDb(options?.Provider);
        var result = await db.UpdateNodeAsync(graphId, nodeId, node);
        return result;
    }

    public async Task<GraphNode> UpsertNodeAsync(string graphId, string nodeId, GraphNodeUpdateModel node, GraphNodeOptions? options = null)
    {
        var db = GetGraphDb(options?.Provider);
        var result = await db.UpsertNodeAsync(graphId, nodeId, node);
        return result;
    }

    public async Task<GraphNodeDeleteResponse?> DeleteNodeAsync(string graphId, string nodeId, GraphNodeOptions? options = null)
    {
        var db = GetGraphDb(options?.Provider);
        var result = await db.DeleteNodeAsync(graphId, nodeId);
        return result;
    }
}
