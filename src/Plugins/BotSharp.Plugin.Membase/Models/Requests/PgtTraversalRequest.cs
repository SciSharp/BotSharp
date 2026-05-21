using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class PgtTraversalRequest
{
    public string StartId { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public PgtTraversalOptions? Options { get; set; }
}

public class PgtTraversalOptions
{
    [JsonPropertyName("max_depth")]
    public int? MaxDepth { get; set; }

    [JsonPropertyName("max_subgraph_nesting")]
    public int? MaxSubGrapNesting { get; set; }

    [JsonPropertyName("max_visits_per_node")]
    public int? MaxVisitsPerNode { get; set; }

    [JsonPropertyName("strategy")]
    public string? Strategy { get; set; }

    [JsonPropertyName("timeout_ms")]
    public int? TimeoutMs { get; set; }

    [JsonPropertyName("node_validation_hooks")]
    public Dictionary<string, object>? NodeValidationHooks { get; set; }

    [JsonPropertyName("edge_validation_hooks")]
    public Dictionary<string, object>? EdgeValidationHooks { get; set; }

    [JsonPropertyName("edge_evaluate_hooks")]
    public Dictionary<string, object>? EdgeEvaluateHooks { get; set; }

    [JsonPropertyName("node_execute_hooks")]
    public Dictionary<string, object>? NodeExecuteHooks { get; set; }

    [JsonPropertyName("traits")]
    public Dictionary<string, object>? Traits { get; set; }

    [JsonPropertyName("interfaces")]
    public Dictionary<string, object>? Interfaces { get; set; }

    [JsonPropertyName("actors")]
    public Dictionary<string, object>? Actors { get; set; }

    [JsonPropertyName("initial_context")]
    public Dictionary<string, object>? InitialContext { get; set; }

    [JsonPropertyName("environment")]
    public Dictionary<string, object>? Environment { get; set; }

    [JsonPropertyName("functions")]
    public Dictionary<string, object>? Functions { get; set; }

    [JsonPropertyName("record_trace")]
    public bool RecordTrace { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("persist_run")]
    public bool PersistRun { get; set; }

    [JsonPropertyName("debug")]
    public bool Debug { get; set; }

    [JsonPropertyName("pause_on")]
    public string[]? PauseOn { get; set; }

    [JsonPropertyName("debug_idle_timeout_ms")]
    public int? DebugIdleTimeoutMs { get; set; }

    [JsonPropertyName("run_id")]
    public string? RunId { get; set; }
}
