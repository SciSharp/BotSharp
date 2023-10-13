namespace BotSharp.Abstraction.Routing.Models;

public class RoutingArgs
{
    [JsonPropertyName("function")]
    public string Function { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "the reason why you select this function or agent";

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("next_action_agent")]
    public string AgentName { get; set; } = "agent for next action based on user's latest response";

    [JsonPropertyName("user_goal_agent")]
    public string OriginalAgent { get; set; } = "agent who can achieve user's original goal";

    public override string ToString()
    {
        var route = string.IsNullOrEmpty(AgentName) ? "" : $"<Route to {AgentName.ToUpper()} because {Reason}>";

        if (string.IsNullOrEmpty(Answer))
        {
            return $"[{Function} {route}]";
        }
        else
        {
            return $"[{Function} {route}] => {Answer}";
        }
    }
}
