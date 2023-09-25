namespace BotSharp.Abstraction.Routing.Models;

public class RoutingArgs
{
    [JsonPropertyName("agent")]
    public string AgentName { get; set; } = string.Empty;

    public override string ToString()
    {
        return AgentName;
    }
}
