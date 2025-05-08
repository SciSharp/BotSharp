using BotSharp.Abstraction.Users.Dtos;
using BotSharp.Abstraction.Users.Enums;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserViewModel : UserDto
{
    [JsonPropertyName("agent_actions")]
    public IEnumerable<UserAgentActionViewModel> AgentActions { get; set; } = [];

    public static UserViewModel FromUser(User user)
    {
        if (user == null)
        {
            return new UserViewModel
            {
                FirstName = "Unknown",
                LastName = "Anonymous",
                Type = UserType.Client,
                Role = AgentRole.User
            };
        }

        return new UserViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = !string.IsNullOrWhiteSpace(user.Phone) ? user.Phone.Replace("+86", String.Empty) : user.Phone,
            Type = user.Type,
            Role = user.Role,
            Source = user.Source,
            ExternalId = user.ExternalId,
            Permissions = user.Permissions,
            AgentActions = user.AgentActions?.Select(x => UserAgentActionViewModel.ToViewModel(x)) ?? [],
            CreateDate = user.CreatedTime,
            UpdateDate = user.UpdatedTime,
            Avatar = "/user/avatar",
            RegionCode = string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode
        };
    }
}
