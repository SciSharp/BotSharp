namespace BotSharp.Abstraction.Routing.Models;

public class RoutingArgs
{
    [JsonPropertyName("function")]
    public string Function { get; set; }

    /// <summary>
    /// The reason why you select this function or agent
    /// </summary>
    [JsonPropertyName("next_action_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextActionReason { get; set; }

    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    [JsonPropertyName("conversation_end")]
    public bool ConversationEnd { get; set; }

    [JsonPropertyName("task_completed")]
    public bool TaskCompleted { get; set; }

    [JsonPropertyName("is_new_task")]
    public bool IsNewTask { get; set; }

    /// <summary>
    /// The content of replying to user
    /// </summary>
    [JsonPropertyName("response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Response { get; set; }

    /// <summary>
    /// Agent for next action based on user latest response
    /// </summary>
    [JsonPropertyName("next_action_agent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string AgentName { get; set; }

    /// <summary>
    /// Agent who can achieve user original goal
    /// </summary>
    [JsonPropertyName("user_goal_agent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string OriginalAgent { get; set; }

    [JsonPropertyName("user_goal_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string UserGoal { get; set; }

    [JsonPropertyName("user_message_in_english")]
    public string UserMessageInEnglish { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = LanguageType.UNKNOWN;

    public override string ToString()
    {
        var route = string.IsNullOrEmpty(AgentName) ? "" : $"<Route to {AgentName.ToUpper()} because {NextActionReason}>";

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
