using BotSharp.Abstraction.Graph.Models;

namespace BotSharp.Plugin.Membase.Models.Graph;

public class Node
{
    public string Id { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public object Properties { get; set; } = new();
    public NodeEmbedding? Embedding { get; set; }
    public DateTime? Time { get; set; } = DateTime.UtcNow;

    public GraphNode ToGraphNode()
    {
        return new GraphNode
        {
            Id = Id,
            Labels = Labels ?? [],
            Properties = Properties ?? new(),
            Time = Time
        };
    }

    public override string ToString()
    {
        var labelsString = Labels.Count > 0 ? string.Join(", ", Labels) : "No Labels";
        return $"Node ({labelsString}: {Id})";
    }
}
