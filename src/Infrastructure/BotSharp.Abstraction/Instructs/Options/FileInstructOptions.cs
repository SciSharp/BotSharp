namespace BotSharp.Abstraction.Instructs.Options;

public class FileInstructOptions
{
    /// <summary>
    /// File processor provider
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("processor")]
    public string? Processor { get; set; }
}
