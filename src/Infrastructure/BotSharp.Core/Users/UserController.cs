using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Core.Users.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Core.Users;

[Authorize]
[ApiController]
public class UserController : ControllerBase, IApiAdapter
{
    private readonly IUserService _userService;
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("/token")]
    public async Task<ActionResult<Token>> GetToken()
    {
        var authcode = Request.Headers["Authorization"].ToString();
        var token = await _userService.GetToken(authcode.Split(' ')[1]);
        if (token == null)
        {
            return Unauthorized();
        }
        return Ok(token);
    }

    [AllowAnonymous]
    [HttpPost("/user")]
    public async Task<UserViewModel> CreateUser(UserCreationModel user)
    {
        var createdUser = await _userService.CreateUser(user.ToUser());
        return UserViewModel.FromUser(createdUser);
    }

    [HttpGet("/user/my")]
    public async Task<UserViewModel> GetMyUserProfile()
    {
        var user = await _userService.GetMyProfile();
        return UserViewModel.FromUser(user);
    }
}
