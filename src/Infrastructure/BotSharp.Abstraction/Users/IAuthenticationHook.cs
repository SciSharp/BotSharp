using BotSharp.Abstraction.Users.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace BotSharp.Abstraction.Users;

public interface IAuthenticationHook
{
    /// <summary>
    /// Interupt the authentication process, and return the user object if the user is authenticated
    /// </summary>
    /// <param name="id"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    Task<User> Authenticate(string id, string password)
        => Task.FromResult(new User());

    /// <summary>
    /// Add extra claims to user
    /// </summary>
    /// <param name="claims"></param>
    /// <returns></returns>
    bool AddClaims(List<Claim> claims)
        => true;

    /// <summary>
    /// User authenticated successfully
    /// </summary>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    bool UserAuthenticated(User user, Token token)
        => true;

    Task OAuthCompleted(TicketReceivedContext context)
        => Task.CompletedTask;

    /// <summary>
    /// Bfore user updating
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task UserUpdating(User user)
        => Task.CompletedTask;

    /// <summary>
    /// After user created
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task UserCreated(User user)
        => Task.CompletedTask;

    /// <summary>
    /// Reset password
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task SendVerificationCode(User user)
        => Task.CompletedTask;

    /// <summary>
    /// Delete users
    /// </summary>
    /// <param name="userIds"></param>
    /// <returns></returns>
    Task DelUsers(List<string> userIds)
        => Task.CompletedTask;
}
