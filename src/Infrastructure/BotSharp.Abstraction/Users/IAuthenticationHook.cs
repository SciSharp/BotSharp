using BotSharp.Abstraction.Users.Models;
using System.Security.Claims;

namespace BotSharp.Abstraction.Users;

public interface IAuthenticationHook
{
    Task<User> Authenticate(string id, string password);
    void AddClaims(List<Claim> claims);
    void BeforeSending(Token token);
    Task UserCreated(User user);
    Task VerificationCodeResetPassword(User user);
    Task DelUsers(List<string> userIds);
}
