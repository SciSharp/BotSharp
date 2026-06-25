using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class ProcedureExecuteResponse
{
    public string[] Columns { get; set; } = [];
    public Dictionary<string, object?>[] Data { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PgtStatistics? Statistics { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ExecutionPlan { get; set; }

    public CypherNotification[] Notifications { get; set; } = [];
    public int RowCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ExecutedAt { get; set; }
}
