namespace BotSharp.Plugin.Membase.Models;

public class NodeUpdateModel
{
    public string Id { get; set; } = null!;
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public NodeEmbedding? Embedding { get; set; }
    public DateTime? Time { get; set; }
}