using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class PgtTraversalRequest
{
    [JsonPropertyName("startId")]
    public string StartId { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public PgtTraversalOptions? Options { get; set; }

    /// <summary>
    /// Builds a traverse request from a fetched <see cref="PgtDefinition"/>,
    /// merging caller overrides on top of stored config values.
    /// </summary>
    public static PgtTraversalRequest FromDefinition(
        PgtDefinition definition,
        string? runId = null,
        Dictionary<string, object?>? environmentOverrides = null,
        Dictionary<string, object?>? initialContextOverrides = null,
        bool stream = false,
        bool debug = false,
        string[]? pauseOn = null,
        int? debugIdleTimeoutMs = null)
    {
        var cfg = definition.Config;

        return new PgtTraversalRequest
        {
            StartId = cfg.StartId ?? string.Empty,
            Options = new PgtTraversalOptions
            {
                MaxDepth = cfg.MaxDepth,
                MaxVisitsPerNode = cfg.MaxVisitsPerNode,
                TimeoutMs = cfg.TimeoutMs,
                MaxSubGrapNesting = cfg.MaxSubgraphNesting,
                Strategy = cfg.Strategy,
                RecordTrace = cfg.RecordTrace,
                PersistRun = cfg.PersistRun,
                RunId = runId,
                Stream = stream,
                Debug = debug,
                PauseOn = pauseOn,
                DebugIdleTimeoutMs = debugIdleTimeoutMs,
                Actors = ParseActorsJson(cfg.ActorsJson),
                Environment = MergeJsonDict(cfg.EnvironmentJson, environmentOverrides),
                InitialContext = MergeJsonDict(cfg.InitialContextJson, initialContextOverrides),
            },
        };
    }

    private static Dictionary<string, object>? ParseActorsJson(string? actorsJson)
    {
        if (string.IsNullOrWhiteSpace(actorsJson))
            return null;

        var array = JsonSerializer.Deserialize<JsonElement[]>(actorsJson);
        if (array is null || array.Length == 0)
            return null;

        var dict = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var element in array)
        {
            if (!element.TryGetProperty("actor_id", out var idProp))
                continue;

            var actorId = idProp.GetString() ?? string.Empty;
            dict[actorId] = JsonSerializer.Deserialize<object>(element.GetRawText()) ?? element;
        }

        return dict.Count > 0 ? dict : null;
    }

    private static Dictionary<string, object>? MergeJsonDict(
        string? existingJson,
        Dictionary<string, object?>? overrides)
    {
        var merged = new Dictionary<string, object>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(existingJson))
        {
            var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
            if (existing is not null)
            {
                foreach (var kv in existing)
                    merged[kv.Key] = kv.Value;
            }
        }

        if (overrides is not null && overrides.Count > 0)
        {
            foreach (var kv in overrides)
            {
                if (kv.Value is null)
                    merged.Remove(kv.Key);
                else
                    merged[kv.Key] = kv.Value;
            }
        }

        return merged.Count > 0 ? merged : null;
    }
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
