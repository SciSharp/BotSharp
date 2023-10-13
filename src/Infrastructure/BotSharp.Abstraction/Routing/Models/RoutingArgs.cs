namespace BotSharp.Abstraction.Routing.Models;

public class RoutingArgs
{
    [JsonPropertyName("function")]
    public string Function { get; set; }

    /// <summary>
    /// The reason why you select this function or agent
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// The content of replying to user
    /// </summary>
    [JsonPropertyName("response")]
    public string Response { get; set; }

    /// <summary>
    /// Agent for next action based on user latest response
    /// </summary>
    [JsonPropertyName("next_action_agent")]
    public string AgentName { get; set; }

    /// <summary>
    /// Agent who can achieve user original goal
    /// </summary>
    [JsonPropertyName("user_goal_agent")]
    public string OriginalAgent { get; set; }

    public override string ToString()
    {
        var route = string.IsNullOrEmpty(AgentName) ? "" : $"<Route to {AgentName.ToUpper()} because {Reason}>";

        if (string.IsNullOrEmpty(Response))
        {
            return $"[{Function} {route}]";
        }
        else
        {
            return $"[{Function} {route}] => {Response}";
        }
    }
}
