using BotSharp.Abstraction.Routing.Models;
using System.Text.Json;

namespace BotSharp.Abstraction.Functions.Models;

public class FunctionCallFromLlm : RoutingArgs
{
    [JsonPropertyName("question")]
    public string? Question { get; set; }

    [JsonPropertyName("args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonDocument? Arguments { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool ExecutingDirectly { get; set; }

    /// <summary>
    /// Router routed to a wrong agent.
    /// Set this flag as True will force router to re-route current request to a new agent.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool UnmatchedAgent { get; set; }

    public override string ToString()
    {
        var route = string.IsNullOrEmpty(AgentName) ? "" : $"<Route to {AgentName.ToUpper()} because {Reason}>";

        if (string.IsNullOrEmpty(Response))
        {
            return $"[{Function} {route} {JsonSerializer.Serialize(Arguments)}]: {Question}";
        }
        else
        {
            return $"[{Function} {route} {JsonSerializer.Serialize(Arguments)}]: {Question} => {Response}";
        }
    }
}
