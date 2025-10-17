namespace BotSharp.Abstraction.Instructs.Options;

public class CodeInstructOptions
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("processor")]
    public string? Processor { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("code_script_name")]
    public string? CodeScriptName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("arguments")]
    public List<KeyValue>? Arguments { get; set; }
}
