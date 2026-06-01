using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class PgtValidationResponse
{
    public string[] Columns { get; set; } = [];
    public PgtValidationDataItem[] Data { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PgtStatistics? Statistics { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ExecutionPlan { get; set; }
    public CypherNotification[] Notifications { get; set; } = [];
    public int RowCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ExecutedAt { get; set; }
}

public class PgtValidationDataItem
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("errors")]
    public object[] Errors { get; set; } = [];

    [JsonPropertyName("warnings")]
    public object[] Warnings { get; set; } = [];

    [JsonPropertyName("stats")]
    public Dictionary<string, object>? Stats { get; set; }
}
