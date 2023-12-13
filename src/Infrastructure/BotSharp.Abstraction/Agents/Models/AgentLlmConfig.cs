namespace BotSharp.Abstraction.Agents.Models;

public class AgentLlmConfig
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Provider { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Model { get; set; }
}
