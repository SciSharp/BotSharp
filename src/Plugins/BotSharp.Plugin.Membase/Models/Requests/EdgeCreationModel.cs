namespace BotSharp.Plugin.Membase.Models;

public class EdgeCreationModel
{
    public string? Id { get; set; }
    public string SourceNodeId { get; set; } = null!;
    public string TargetNodeId { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool Directed { get; set; } = true;
    public float? Weight { get; set; } = 1.0f;
    public object? Properties { get; set; }
}
