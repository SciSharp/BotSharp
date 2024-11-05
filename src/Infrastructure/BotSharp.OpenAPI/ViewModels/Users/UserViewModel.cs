using BotSharp.Abstraction.Users.Enums;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserViewModel
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
    public string Type { get; set; } = UserType.Client;
    public string Role { get; set; } = UserRole.User;
    [JsonPropertyName("full_name")]
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? Source { get; set; }
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
    public string Avatar { get; set; } = "/user/avatar";
    [JsonPropertyName("create_date")]
    public DateTime CreateDate { get; set; }
    [JsonPropertyName("update_date")]
    public DateTime UpdateDate { get; set; }

    public string RegionCode { get; set; } = "CN";

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
            Phone = (!string.IsNullOrWhiteSpace(user.Phone) && user.Phone.Length > 3) ? (user.Phone.Substring(0, 3) == "+86" ? user.Phone.Substring(3) : user.Phone) : user.Phone,
            Type = user.Type,
            Role = user.Role,
            Source = user.Source,
            ExternalId = user.ExternalId,
            CreateDate = user.CreatedTime,
            UpdateDate = user.UpdatedTime,
            Avatar = "/user/avatar",
            RegionCode = string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode
        };
    }
}
