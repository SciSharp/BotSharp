using BotSharp.Abstraction.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Abstraction.Infrastructures.Attributes;

/// <summary>
/// BotSharp authorization: check whether the request user is admin or root role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BotSharpAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public BotSharpAuthAttribute()
    {
        
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;

        var userIdentity = services.GetRequiredService<IUserIdentity>();
        var userService = services.GetRequiredService<IUserService>();

        var (isAdmin, user) = await userService.IsAdminUser(userIdentity.Id);
        if (!isAdmin || user == null)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
