namespace BotSharp.Plugin.Membase.Models.Graph;

public class Edge
{
    public string Id { get; set; } = string.Empty;
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? Properties { get; set; }
    public string? Direction { get; set; }
    public bool? Directed { get; set; }
    public float? Weight { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
