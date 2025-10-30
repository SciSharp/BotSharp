namespace BotSharp.Abstraction.Agents.Settings;

public class AgentSettings
{
    public string DataDir { get; set; } = "agents";
    public string TemplateFormat { get; set; } = "liquid";
    public string HostAgentId { get; set; } = string.Empty;
    public bool EnableTranslator { get; set; } = false;

    /// <summary>
    /// This is the default LLM config for agent
    /// </summary>
    public AgentLlmConfig LlmConfig { get; set; } = new AgentLlmConfig();

    /// <summary>
    /// General coding settings
    /// </summary>
    public CodingSettings Coding { get; set; } = new CodingSettings();
}
