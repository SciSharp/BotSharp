using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserUpdateModel
{
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Type { get; set; }
    public string? Role { get; set; }
    public string? Source { get; set; }

    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    public IEnumerable<string> Permissions { get; set; } = [];

    [JsonPropertyName("agent_actions")]
    public IEnumerable<UserAgentActionViewModel> AgentActions { get; set; } = [];

    public static User ToUser(UserUpdateModel model)
    {
        return new User
        {
            Id = model.Id,
            UserName = model.UserName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            Type = model.Type,
            Role = model.Role,
            Source = model.Source,
            ExternalId = model.ExternalId,
            Permissions = model.Permissions,
            AgentActions = model.AgentActions?.Select(x => UserAgentActionViewModel.ToDomainModel(x)) ?? []
        };
    }
}
