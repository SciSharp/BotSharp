using BotSharp.Abstraction.Users;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BotSharp.Core.Users.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _contextAccessor;
    private IEnumerable<Claim> _claims => _contextAccessor.HttpContext.User.Claims;

    public CurrentUser(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public string Id => _claims.First(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;


    public string Email => _claims.First(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value;

    public string FirstName => _claims.First(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value;

    public string LastName => _claims.First(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value;
}
