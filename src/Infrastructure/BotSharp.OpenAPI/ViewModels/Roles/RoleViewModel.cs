using BotSharp.Abstraction.Roles.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Roles;

public class RoleViewModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("permissions")]
    public IEnumerable<string> Permissions { get; set; } = [];

    [JsonPropertyName("agent_actions")]
    public IEnumerable<RoleAgentActionViewModel> AgentActions { get; set; } = [];

    [JsonPropertyName("create_date")]
    public DateTime? CreateDate { get; set; }

    [JsonPropertyName("update_date")]
    public DateTime? UpdateDate { get; set; }

    public static RoleViewModel FromRole(Role? role)
    {
        if (role == null) return null;

        return new RoleViewModel
        {
            Id = role.Id,
            Name = role.Name,
            Permissions = role.Permissions,
            AgentActions = role.AgentActions?.Select(x => RoleAgentActionViewModel.ToViewModel(x)) ?? [],
            CreateDate = role.CreatedTime != default ? role.CreatedTime : null,
            UpdateDate = role.UpdatedTime != default ? role.UpdatedTime : null
        };
    }
}
