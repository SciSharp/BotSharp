using BotSharp.Abstraction.Roles.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Roles;

public class RoleUpdateModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("permissions")]
    public IEnumerable<string> Permissions { get; set; } = [];

    [JsonPropertyName("agent_actions")]
    public IEnumerable<RoleAgentActionViewModel> AgentActions { get; set; } = [];

    public static Role ToRole(RoleUpdateModel model)
    {
        return new Role
        {
            Id = model.Id,
            Name = model.Name,
            Permissions = model.Permissions,
            AgentActions = model.AgentActions?.Select(x => RoleAgentActionViewModel.ToDomainModel(x)) ?? []
        };
    }
}
