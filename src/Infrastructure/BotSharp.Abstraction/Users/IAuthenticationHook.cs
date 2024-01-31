using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Users;

public interface IAuthenticationHook
{
    Task<User> Authenticate(string id, string password);
}
