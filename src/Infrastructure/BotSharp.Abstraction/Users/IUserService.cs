using BotSharp.Abstraction.Users.Models;
using BotSharp.OpenAPI.ViewModels.Users;

namespace BotSharp.Abstraction.Users;

public interface IUserService
{
    Task<User> GetUser(string id);
    Task<User> CreateUser(User user);
    Task<Token> ActiveUser(UserActivationModel model);
    Task<Token?> GetToken(string authorization);
    Task<User> GetMyProfile();
    Task<bool> VerifyUserUnique(string userName);
    Task<bool> VerifyEmailUnique(string email);
}