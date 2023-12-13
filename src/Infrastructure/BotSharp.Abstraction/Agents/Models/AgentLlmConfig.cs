namespace BotSharp.Abstraction.Agents.Models;

public class AgentLlmConfig
{
    /// <summary>
    /// Completion Provider
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Model name
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
