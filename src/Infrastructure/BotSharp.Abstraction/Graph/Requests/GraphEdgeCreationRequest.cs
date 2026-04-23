namespace BotSharp.Abstraction.Graph.Requests;

public class GraphEdgeCreationRequest
{
    public string? Id { get; set; }
    public string SourceNodeId { get; set; } = null!;
    public string TargetNodeId { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool Directed { get; set; } = true;
    public float? Weight { get; set; } = 1.0f;
    public Dictionary<string, object>? Properties { get; set; }
}
