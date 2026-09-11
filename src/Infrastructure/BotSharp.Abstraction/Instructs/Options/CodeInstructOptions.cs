namespace BotSharp.Abstraction.Instructs.Options;

public class CodeInstructOptions
{
    /// <summary>
    /// Skip the code execution and go straight to the llm completion
    /// </summary>
    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

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
}
