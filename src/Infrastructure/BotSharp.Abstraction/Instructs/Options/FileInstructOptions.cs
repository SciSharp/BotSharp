namespace BotSharp.Abstraction.Instructs.Options;

public class FileInstructOptions
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("processor")]
    public string? Processor { get; set; }
}
