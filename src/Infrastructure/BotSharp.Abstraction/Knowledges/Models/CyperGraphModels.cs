namespace BotSharp.Abstraction.Knowledges.Models;

public class GraphQueryResult
{
    public string[] Columns { get; set; } = [];
    public Dictionary<string, object?>[] Items { get; set; } = [];
}

public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public object Properties { get; set; } = new();
    public DateTime Time { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        var labelsString = Labels.Count > 0 ? string.Join(", ", Labels) : "No Labels";
        return $"Node ({labelsString}: {Id})";
    }
}
