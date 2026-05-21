using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class PgtTraversalResponse
{
    public string[] Columns { get; set; } = [];
    public PgtTraversalDataItem[] Data { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PgtStatistics? Statistics { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ExecutionPlan { get; set; }
    public CypherNotification[] Notifications { get; set; } = [];
    public int RowCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ExecutedAt { get; set; }

    public string RunId { get; set; }
}

public class PgtTraversalDataItem
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
