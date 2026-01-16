namespace BotSharp.Abstraction.Graph.Requests;

public class GraphNodeCreationModel
{
    public string? Id { get; set; }
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public GraphNodeEmbedding? Embedding { get; set; }
    public DateTime? Time { get; set; }
}

public class GraphNodeEmbedding
{
    public string Model { get; set; }
    public float[] Vector { get; set; }
}