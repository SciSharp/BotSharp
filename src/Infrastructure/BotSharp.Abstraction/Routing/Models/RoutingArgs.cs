namespace BotSharp.Abstraction.Routing.Models;

public class RoutingArgs
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("agent")]
    public string AgentName { get; set; } = string.Empty;

    public override string ToString()
    {
        return string.IsNullOrEmpty(AgentName) ? "" : $"<Route to {AgentName.ToUpper()} because {Reason}>";
    }
}
