using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Users.Dtos;

public class UserDto
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

    public IEnumerable<string> Permissions { get; set; } = [];

    [JsonPropertyName("create_date")]
    public DateTime CreateDate { get; set; }

    [JsonPropertyName("update_date")]
    public DateTime UpdateDate { get; set; }

    public string RegionCode { get; set; } = "CN";

    public static UserDto FromUser(User user)
    {
        if (user == null)
        {
            return new UserDto
            {
                FirstName = "Unknown",
                LastName = "Anonymous",
                Type = UserType.Client,
                Role = AgentRole.User
            };
        }

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = !string.IsNullOrWhiteSpace(user.Phone) ? user.Phone.Replace("+86", string.Empty) : user.Phone,
            Type = user.Type,
            Role = user.Role,
            Source = user.Source,
            ExternalId = user.ExternalId,
            Permissions = user.Permissions,
            CreateDate = user.CreatedTime,
            UpdateDate = user.UpdatedTime,
            Avatar = "/user/avatar",
            RegionCode = string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode
        };
    }
}
