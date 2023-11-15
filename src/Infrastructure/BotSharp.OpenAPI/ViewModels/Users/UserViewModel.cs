using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserViewModel
{
    public string Id { get; set; }
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }
    [JsonPropertyName("last_name")]
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; } = UserRole.Client;
    [JsonPropertyName("full_name")]
    public string FullName => $"{FirstName} {LastName}";

    public static UserViewModel FromUser(User user)
    {
        if (user == null)
        {
            return new UserViewModel
            {
                FirstName = "AI",
                LastName = "Assistant",
                Role = AgentRole.Assistant
            };
        }

        return new UserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role
        };
    }
}
