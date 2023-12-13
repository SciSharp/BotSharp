namespace BotSharp.Abstraction.Agents.Models;

public class AgentLlmConfig
{
    /// <summary>
    /// Completion Provider
    /// </summary>
    [JsonPropertyName("provider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Provider { get; set; }

    /// <summary>
    /// Model name
    /// </summary>
    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }
}
