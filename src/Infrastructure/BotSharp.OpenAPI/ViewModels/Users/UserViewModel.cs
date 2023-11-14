using BotSharp.Abstraction.Users.Models;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserViewModel
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public static UserViewModel FromUser(User user)
    {
        return new UserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };
    }
}
