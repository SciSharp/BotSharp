using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class PgtSimulationRequest
{
    public string StartId { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public PgtSimulationOptions? Options { get; set; }
}

public class PgtSimulationOptions
{
    [JsonPropertyName("max_depth")]
    public int? MaxDepth { get; set; }

    [JsonPropertyName("timeout_ms")]
    public int? TimeoutMs { get; set; }

    [JsonPropertyName("strategy")]
    public string? Strategy { get; set; }

    [JsonPropertyName("initial_context")]
    public Dictionary<string, object>? InitialContext { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("run_id")]
    public string? RunId { get; set; }

    [JsonPropertyName("persist_run")]
    public bool PersistRun { get; set; }

    [JsonPropertyName("debug")]
    public bool Debug { get; set; }

    [JsonPropertyName("pause_on")]
    public string[]? PauseOn { get; set; }

    [JsonPropertyName("debug_idle_timeout_ms")]
    public int? DebugIdleTimeoutMs { get; set; }

    [JsonPropertyName("node_execute_hooks")]
    public Dictionary<string, object>? NodeExecuteHooks { get; set; }
}
