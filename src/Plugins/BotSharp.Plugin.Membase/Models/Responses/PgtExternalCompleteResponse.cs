using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Membase.Models;

public class PgtExternalCompleteResponse
{
    public PgtExternalTask Task { get; set; } = new();

    [JsonPropertyName("already_completed")]
    public bool AlreadyCompleted { get; set; }
}

public class PgtExternalTask
{
    public string TaskId { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
    public string GraphId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Request { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Response { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Error { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public long NotBefore { get; set; }
}
