namespace BotSharp.Plugin.CodeAct.Functions;

public class ExecuteCodeArgs
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "python";

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    [JsonPropertyName("read_only")]
    public bool ReadOnly { get; set; } = true;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
