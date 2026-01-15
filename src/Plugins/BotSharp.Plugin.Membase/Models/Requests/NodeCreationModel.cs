using BotSharp.Abstraction.Graph.Requests;

namespace BotSharp.Plugin.Membase.Models;

public class NodeCreationModel
{
    public string? Id { get; set; }
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public NodeEmbedding? Embedding { get; set; }
    public DateTime? Time { get; set; }

    public static NodeCreationModel From(GraphNodeCreationModel request)
    {
        return new NodeCreationModel
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

public class NodeEmbedding
{
    public string Model { get; set; }
    public float[] Vector { get; set; }
}