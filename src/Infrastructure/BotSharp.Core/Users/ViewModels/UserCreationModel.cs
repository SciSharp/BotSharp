using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Core.Users.ViewModels;

public class UserCreationModel
{
    public string FirstName { get; set; }   
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    public User ToUser()
    {
        return new User 
        { 
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Password = Password
        };
    }
}
