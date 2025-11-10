namespace BotSharp.Abstraction.Instructs.Options;

public class CodeInstructOptions
{
    /// <summary>
    /// Code processor provider
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("processor")]
    public string? Processor { get; set; }

    /// <summary>
    /// Code script name
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("script_name")]
    public string? ScriptName { get; set; }

    /// <summary>
    /// Code script name
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("script_type")]
    public string? ScriptType { get; set; }

    /// <summary>
    /// Arguments
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("arguments")]
    public List<KeyValue>? Arguments { get; set; }

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("timeout_seconds")]
    public int? TimeoutSeconds { get; set; }
}
