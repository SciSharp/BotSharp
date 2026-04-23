namespace BotSharp.Abstraction.Graph.Models;

public class GraphNodeModel
{
    public string Id { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public object Properties { get; set; } = new();
    public DateTime? Time { get; set; } = DateTime.UtcNow;
}
