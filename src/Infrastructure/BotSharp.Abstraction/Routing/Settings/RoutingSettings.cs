namespace BotSharp.Abstraction.Routing.Settings;

public class RoutingSettings
{
    /// <summary>
    /// Router Agent Id
    /// </summary>
    public string RouterId { get; set; } = string.Empty;

    public string RouteName { get; set; } = "Router";

    public bool EnableReasoning {  get; set; } = false;

    public string Provider {  get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
}
