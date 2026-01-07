namespace BotSharp.Plugin.Membase.Models;

public class NodeCreationModel
{
    public string? Id { get; set; }
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public EmbeddingInfo? Embedding { get; set; }
    public DateTime? Time { get; set; }

    public Node ToNode()
    {
        return new Node
        {
            Id = Id,
            Labels = Labels?.ToList() ?? new List<string>(),
            Properties = Properties ?? new(),
            Embedding = Embedding,
            Time = Time ?? DateTime.UtcNow
        };
    }
}

public class EmbeddingInfo
{
    public string Model { get; set; }
    public float[] Vector { get; set; }
}