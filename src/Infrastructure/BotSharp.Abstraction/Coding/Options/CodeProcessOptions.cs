namespace BotSharp.Abstraction.Coding.Options;

public class CodeProcessOptions : CodeGenerationOptions
{
    /// <summary>
    /// Code processor provider
    /// </summary>
    [JsonPropertyName("processor")]
    public string? Processor { get; set; }

    /// <summary>
    /// Whether to save the generated code script to db
    /// </summary>
    [JsonPropertyName("save_to_db")]
    public bool SaveToDb { get; set; }

    /// <summary>
    /// Code script name (e.g., demo.py)
    /// </summary>
    [JsonPropertyName("script_name")]
    public string? ScriptName { get; set; }

    /// <summary>
    /// Code script type (i.e., src, test)
    /// </summary>
    [JsonPropertyName("script_type")]
    public string? ScriptType { get; set; } = AgentCodeScriptType.Src;
}
