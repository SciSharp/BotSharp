namespace BotSharp.Plugin.Membase.Models;

public class NodeCreationModel
{
    public string? Id { get; set; }
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public NodeEmbedding? Embedding { get; set; }
    public DateTime? Time { get; set; }
}

public class NodeEmbedding
{
    public string Model { get; set; }
    public float[] Vector { get; set; }
}