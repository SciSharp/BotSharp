using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserCreationModel
{
    public string? UserName { get; set; }
    public string FirstName { get; set; } = string.Empty;   
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = UserRole.Client;

    public User ToUser()
    {
        return new User 
        { 
            UserName = UserName,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Phone = Phone,
            Password = Password,
            Role = Role
        };
    }
}
