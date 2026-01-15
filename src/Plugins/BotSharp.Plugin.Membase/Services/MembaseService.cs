using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Plugin.Membase.Services;

public class MembaseService
{
    private readonly IServiceProvider _services;
    private readonly IMembaseApi _membase;

    public MembaseService(IServiceProvider services, IMembaseApi membase)
    {
        _services = services;
        _membase = membase;
    }

    public async Task<GraphQueryResult> Execute(string graphId, string query, Dictionary<string, object>? args = null)
    {
        var response = await _membase.CypherQueryAsync(graphId, new CypherQueryRequest
        {
            Query = query,
            Parameters = args ?? []
        });

        return new GraphQueryResult
        {
            Columns = response.Columns,
            Items = response.Data
        };
    }

    public async Task<GraphNode> MergeNode(string graphId, GraphNode node)
    {
        var newNode = await _membase.MergeNodeAsync(graphId, node.Id, new NodeUpdateModel
        {
            Id = node.Id,
            Labels = [.. node.Labels],
            Properties = node.Properties,
            Time = node.Time
        });

        return node;
    }

    public async Task<bool> DeleteNode(string graphId, string nodeId)
    {
        await _membase.DeleteNodeAsync(graphId, nodeId);
        return true;
    }
}
