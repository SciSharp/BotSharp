namespace BotSharp.Abstraction.Agents.Settings;

public class AgentSettings
{
    public string DataDir { get; set; } = string.Empty;
    public string TemplateFormat { get; set; } = "liquid";
    public string HostAgentId { get; set; } = string.Empty;

    /// <summary>
    /// This is the default LLM config for agent
    /// </summary>
    public AgentLlmConfig LlmConfig { get; set; }
        = new AgentLlmConfig();
}
