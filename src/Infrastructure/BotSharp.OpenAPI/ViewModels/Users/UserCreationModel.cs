using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserCreationModel
{
    public string FirstName { get; set; } = string.Empty;   
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = UserRole.Client;

    public User ToUser()
    {
        return new User 
        { 
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Password = Password,
            Role = Role
        };
    }
}
