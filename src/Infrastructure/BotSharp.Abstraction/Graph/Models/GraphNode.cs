namespace BotSharp.Abstraction.Graph.Models;

public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public object Properties { get; set; } = new();
    public DateTime? Time { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        var labelsString = Labels.Count > 0 ? string.Join(", ", Labels) : "No Labels";
        return $"Node ({labelsString}: {Id})";
    }
}
