namespace BotSharp.Abstraction.Graph.Requests;

public class GraphNodeUpdateModel
{
    public string Id { get; set; } = null!;
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public GraphNodeEmbedding? Embedding { get; set; }
    public DateTime? Time { get; set; }
}
