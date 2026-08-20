namespace BotSharp.Abstraction.Routing.Models;

public class RoutingArgs
{
    [JsonPropertyName("function")]
    public string Function { get; set; } = "route_to_agent";

    /// <summary>
    /// The reason why you select this function or agent
    /// </summary>
    [JsonPropertyName("next_action_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextActionReason { get; set; }

    [JsonPropertyName("conversation_end")]
    public bool ConversationEnd { get; set; }

    /// <summary>
    /// Agent for next action based on user latest response
    /// </summary>
    [JsonPropertyName("next_action_agent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Agent who can achieve user original goal
    /// </summary>
    [Obsolete("Will be replaced by dedicate Reasoner")]
    [JsonPropertyName("user_goal_agent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string OriginalAgent { get; set; } = string.Empty;

    [Obsolete("Will be replaced by dedicate Reasoner")]
    [JsonPropertyName("user_goal_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string UserGoal { get; set; } = string.Empty;

    public override string ToString()
    {
        var route = string.IsNullOrEmpty(AgentName) ? "" : $"<Route to {AgentName.ToUpper()} because {NextActionReason}>";

        return $"[{Function} {route}]";
    }
}
