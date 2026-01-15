using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Graph.Requests;
using BotSharp.Abstraction.Graph.Responses;

namespace BotSharp.Plugin.Membase.GraphDb;

public partial class MembaseGraphDb
{
    public async Task<GraphNode?> GetNodeAsync(string graphId, string nodeId)
    {
        var found = await _membaseApi.GetNodeAsync(graphId, nodeId);
        if (found == null)
        {
            return null;
        }

        return new GraphNode
        {
            Id = found.Id,
            Labels = found.Labels,
            Properties = found.Properties,
            Time = found.Time
        };
    }

    public async Task<GraphNode> CreateNodeAsync(string graphId, GraphNodeCreationModel node)
    {
        var model = NodeCreationModel.From(node);
        var createdNode = await _membaseApi.CreateNodeAsync(graphId, model);
        return createdNode.ToGraphNode();
    }

    public async Task<GraphNode> UpdateNodeAsync(string graphId, string nodeId, GraphNodeUpdateModel node)
    {
        var model = NodeUpdateModel.From(node);
        var updatedNode = await _membaseApi.UpdateNodeAsync(graphId, nodeId, model);
        return updatedNode.ToGraphNode();
    }

    public async Task<GraphNode> UpsertNodeAsync(string graphId, string nodeId, GraphNodeUpdateModel node)
    {
        var model = NodeUpdateModel.From(node);
        var updatedNode = await _membaseApi.MergeNodeAsync(graphId, nodeId, model);
        return updatedNode.ToGraphNode();
    }

    public async Task<GraphNodeDeleteResponse?> DeleteNodeAsync(string graphId, string nodeId)
    {
        try
        {
            var response = await _membaseApi.DeleteNodeAsync(graphId, nodeId);
            return new GraphNodeDeleteResponse { Success = response != null, Message = response?.Message };
        }
        catch (Exception ex)
        {
            return new GraphNodeDeleteResponse
            {
                ErrorMsg = ex.Message
            };
        }
    }
}
