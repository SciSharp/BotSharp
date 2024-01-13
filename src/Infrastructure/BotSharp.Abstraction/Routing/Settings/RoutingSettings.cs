namespace BotSharp.Abstraction.Routing.Settings;

public class RoutingSettings
{
    /// <summary>
    /// Router Agent Id
    /// </summary>
    public string[] AgentIds { get; set; } = new string[0];

    public string Planner { get; set; } = string.Empty;
}
