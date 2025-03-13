using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Roles.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Roles;

public class RoleAgentActionViewModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

    [JsonPropertyName("agent")]
    public Agent? Agent { get; set; }

    [JsonPropertyName("actions")]
    public IEnumerable<string> Actions { get; set; } = [];


    public static RoleAgentActionViewModel ToViewModel(RoleAgentAction action)
    {
        return new RoleAgentActionViewModel
        {
            Id = action.Id,
            AgentId = action.AgentId,
            Agent = action.Agent,
            Actions = action.Actions
        };
    }

    public static RoleAgentAction ToDomainModel(RoleAgentActionViewModel action)
    {
        return new RoleAgentAction
        {
            Id = action.Id,
            AgentId = action.AgentId,
            Actions = action.Actions
        };
    }
}
