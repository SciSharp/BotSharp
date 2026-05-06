using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class PgtSimulationResponse
{
    public string[] Columns { get; set; } = [];
    public PgtSimulationDataItem[] Data { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PgtSimulationStatistics? Statistics { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ExecutionPlan { get; set; }
    public CypherNotification[] Notifications { get; set; } = [];
    public int RowCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ExecutedAt { get; set; }

    public string RunId { get; set; }
}

public class PgtSimulationDataItem
{
    [JsonPropertyName("final_context")]
    public Dictionary<string, object>? FinalContext { get; set; }

    [JsonPropertyName("visited")]
    public string[] Visited { get; set; } = [];

    [JsonPropertyName("trace_log")]
    public PgtTraceLogEntry[] TraceLog { get; set; } = [];

    [JsonPropertyName("halted")]
    public string Halted { get; set; } = string.Empty;

    [JsonPropertyName("halt_reason")]
    public string HaltReason { get; set; } = string.Empty;
}

public class PgtTraceLogEntry
{
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("node_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NodeId { get; set; }

    [JsonPropertyName("edge_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EdgeId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Target { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Allowed { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Depth { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Output { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Input { get; set; }
}

public class PgtSimulationStatistics
{
    public long ExecutionTimeMs { get; set; }
}
