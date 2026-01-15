using BotSharp.Abstraction.Graph.Requests;

namespace BotSharp.Plugin.Membase.Models;

public class NodeUpdateModel
{
    public string Id { get; set; } = null!;
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public NodeEmbedding? Embedding { get; set; }
    public DateTime? Time { get; set; }

    public static NodeUpdateModel From(GraphNodeUpdateModel request)
    {
        return new NodeUpdateModel
        {
            Id = request.Id,
            Labels = request.Labels,
            Properties = request.Properties,
            Time = request.Time,
            Embedding = request?.Embedding != null ? new NodeEmbedding
            {
                Model = request.Embedding.Model,
                Vector = request.Embedding.Vector
            } : null
        };
    }
}