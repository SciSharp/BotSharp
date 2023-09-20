namespace BotSharp.Abstraction.Routing.Settings;

public class RoutingSettings
{
    /// <summary>
    /// Router Agent Id
    /// </summary>
    public string RouterId { get; set; } = string.Empty;

    public bool EnableReasoning {  get; set; } = false;
}
