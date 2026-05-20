using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class PgtValidationRequest
{
    public string StartId { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public PgtValidationOptions? Options { get; set; }
}

public class PgtValidationOptions
{
    [JsonPropertyName("max_depth")]
    public int? MaxDepth { get; set; }

    [JsonPropertyName("max_nodes")]
    public int? MaxNodes { get; set; }

    [JsonPropertyName("allow_cycles")]
    public bool? AllowCycles { get; set; }

    [JsonPropertyName("target_node_ids")]
    public string[]? TargetNodeIds { get; set; }

    [JsonPropertyName("edge_types")]
    public string[]? EdgeTypes { get; set; }

    [JsonPropertyName("node_validation_hooks")]
    public Dictionary<string, object>? NodeValidationHooks { get; set; }

    [JsonPropertyName("edge_validation_hooks")]
    public Dictionary<string, object>? EdgeValidationHooks { get; set; }

    [JsonPropertyName("traits")]
    public Dictionary<string, object>? Traits { get; set; }

    [JsonPropertyName("interfaces")]
    public Dictionary<string, object>? Interfaces { get; set; }

    [JsonPropertyName("actors")]
    public Dictionary<string, object>? Actors { get; set; }

    [JsonPropertyName("initial_context")]
    public Dictionary<string, object>? InitialContext { get; set; }
}
