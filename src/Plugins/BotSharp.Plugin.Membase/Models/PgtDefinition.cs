using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

/// <summary>
/// Response from GET /graph/{graphId}/pgt-definitions/{definitionId}
/// </summary>
public class PgtDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("graphId")]
    public string GraphId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("ownerUserId")]
    public string? OwnerUserId { get; set; }

    [JsonPropertyName("config")]
    public PgtConfig Config { get; set; } = new();

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
