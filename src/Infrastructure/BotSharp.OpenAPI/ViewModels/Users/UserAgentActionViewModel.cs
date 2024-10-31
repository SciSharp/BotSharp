using BotSharp.Abstraction.Agents.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserAgentActionViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

    [JsonPropertyName("agent")]
    public Agent? Agent { get; set; }

    [JsonPropertyName("actions")]
    public IEnumerable<string> Actions { get; set; } = [];

    public static UserAgentActionViewModel ToViewModel(UserAgentAction action)
    {
        return new UserAgentActionViewModel
        {
            Id = action.Id,
            AgentId = action.AgentId,
            Agent = action.Agent,
            Actions = action.Actions
        };
    }
}
