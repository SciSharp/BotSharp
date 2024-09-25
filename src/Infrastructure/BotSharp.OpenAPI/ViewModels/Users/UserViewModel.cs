using BotSharp.Abstraction.Users.Enums;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserViewModel
{
    public string Id { get; set; } = null!;
    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = null!;
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = null!;
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Type { get; set; } = UserType.Client;
    public string Role { get; set; } = UserRole.User;
    [JsonPropertyName("full_name")]
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Source { get; set; }
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
    public string Avatar { get; set; } = "/user/avatar";
    [JsonPropertyName("create_date")]
    public DateTime CreateDate { get; set; }
    [JsonPropertyName("update_date")]
    public DateTime UpdateDate { get; set; }

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
            Phone = user.Phone,
            Type = user.Type,
            Role = user.Role,
            Source = user.Source,
            ExternalId = user.ExternalId,
            CreateDate = user.CreatedTime,
            UpdateDate = user.UpdatedTime,
            Avatar = "/user/avatar"
        };
    }
}
