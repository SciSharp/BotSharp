using BotSharp.Abstraction.Users.Enums;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserViewModel
{
    public string Id { get; set; }
    [JsonPropertyName("user_name")]
    public string UserName { get; set; }
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }
    [JsonPropertyName("last_name")]
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; } = UserRole.Client;
    [JsonPropertyName("full_name")]
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Source { get; set; }
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
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
            Role = user.Role,
            Source = user.Source,
            ExternalId = user.ExternalId,
            CreateDate = user.CreatedTime,
            UpdateDate = user.UpdatedTime
        };
    }
}
