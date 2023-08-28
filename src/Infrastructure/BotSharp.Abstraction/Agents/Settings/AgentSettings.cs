namespace BotSharp.Abstraction.Agents.Settings;

public class AgentSettings
{
    /// <summary>
    /// Router Agent Id
    /// </summary>
    public string RouterId { get; set; }

    /// <summary>
    /// Reasoner Agent Id
    /// </summary>
    public string ReasonerId { get; set; }

    public string DataDir { get; set; }
    public string TemplateFormat { get; set; }
}
