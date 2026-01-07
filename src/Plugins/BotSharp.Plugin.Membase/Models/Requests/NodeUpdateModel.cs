namespace BotSharp.Plugin.Membase.Models;

public class NodeUpdateModel
{
    public string Id { get; set; } = null!;
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public EmbeddingInfo? Embedding { get; set; }
    public DateTime? Time { get; set; }

    public Node ToNode()
    {
        return new Node
        {
            Id = Id,
            Labels = Labels?.ToList() ?? [],
            Properties = Properties ?? new(),
            Embedding = Embedding,
            Time = Time ?? DateTime.UtcNow
        };
    }
}
