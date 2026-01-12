namespace BotSharp.Plugin.Membase.Models;

public class GraphInfo
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? Region { get; set; }
    public bool? SemanticAware { get; set; }
    public string? DefaultEmbeddingModel { get; set; }
    public string? OrgId { get; set; }
    public string? ProjectId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
