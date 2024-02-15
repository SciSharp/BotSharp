using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.ComponentModel.DataAnnotations;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IUserService _userService;
    public UserController(IUserService userService, IServiceProvider services)
    {
        _services = services;
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("/token")]
    public async Task<ActionResult<Token>> GetToken([FromHeader(Name = "Authorization")][Required] string authcode)
    {
        if (authcode.Contains(' '))
        {
            authcode = authcode.Split(' ')[1];
        }

        var token = await _userService.GetToken(authcode);

        if (token == null)
        {
            return Unauthorized();
        }
        return Ok(token);
    }

    [AllowAnonymous]
    [HttpGet("/sso/{provider}")]
    public async Task<IActionResult> Authorize([FromRoute] string provider)
    {
        return Challenge(new AuthenticationProperties { RedirectUri = $"page/user/me" }, provider);
    }

    [AllowAnonymous]
    [HttpGet("/signout")]
    [HttpPost("/signout")]
    public IActionResult SignOutCurrentUser()
    {
        // Instruct the cookies middleware to delete the local cookie created
        // when the user agent is redirected from the external identity provider
        // after a successful authentication flow (e.g Google or Facebook).
        return SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    [HttpPost("/user")]
    public async Task<UserViewModel> CreateUser(UserCreationModel user)
    {
        var createdUser = await _userService.CreateUser(user.ToUser());
        return UserViewModel.FromUser(createdUser);
    }

    [HttpGet("/user/me")]
    public async Task<UserViewModel> GetMyUserProfile()
    {
        var user = await _userService.GetMyProfile();
        if (user == null)
        {
            var identiy = _services.GetRequiredService<IUserIdentity>();
            var accessor = _services.GetRequiredService<IHttpContextAccessor>();
            var claims = accessor.HttpContext.User.Claims;
            if (claims.Any(x => x.Type == GitHubAuthenticationConstants.Claims.Name))
            {
                user = await _userService.CreateUser(new User
                {
                    Email = identiy.Email,
                    UserName = identiy.UserName,
                    FirstName = identiy.FirstName,
                    LastName = identiy.LastName,
                    Source = "GitHub",
                    ExternalId = identiy.Id,
                });
            }
        }
        return UserViewModel.FromUser(user);
    }
}
