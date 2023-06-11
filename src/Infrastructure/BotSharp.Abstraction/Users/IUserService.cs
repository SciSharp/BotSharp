using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Users;

public interface IUserService
{
    Task<User> CreateUser(User user);
    Task<Token> GetToken(string authorization);
    Task<User> GetMyProfile();
}