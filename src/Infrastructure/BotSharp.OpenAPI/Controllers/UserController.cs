using BotSharp.Abstraction.Users.Settings;
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
    private readonly IUserIdentity _user;
    private readonly AccountSetting _setting;

    public UserController(
        IUserService userService,
        IServiceProvider services,
        IUserIdentity user,
        AccountSetting setting)
    {
        _services = services;
        _userService = userService;
        _user = user;
        _setting = setting;
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
    public async Task<IActionResult> Authorize([FromRoute] string provider, string redirectUrl)
    {
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, provider);
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

    [AllowAnonymous]
    [HttpPost("/user/activate")]
    public async Task<ActionResult<Token>> ActivateUser(UserActivationModel model)
    {
        var token = await _userService.ActiveUser(model);
        if (token == null)
        {
            return BadRequest();
        }
        return Ok(token);
    }

    [HttpGet("/user/me")]
    public async Task<UserViewModel> GetMyUserProfile()
    {
        var user = await _userService.GetMyProfile();
        if (user == null && _setting.CreateUserAutomatically)
        {
            var identiy = _services.GetRequiredService<IUserIdentity>();
            var accessor = _services.GetRequiredService<IHttpContextAccessor>();
            var claims = accessor.HttpContext.User.Claims;
            user = await _userService.CreateUser(new User
            {
                Email = identiy.Email,
                UserName = identiy.UserName,
                FirstName = identiy.FirstName,
                LastName = identiy.LastName,
                Source = claims.First().Issuer,
                ExternalId = identiy.Id,
                RegionCode = identiy.RegionCode,
                Phone = identiy.Phone,
            });
        }
        return UserViewModel.FromUser(user);
    }

    [AllowAnonymous]
    [HttpGet("/user/name/existing")]
    public async Task<bool> VerifyUserNameExisting([FromQuery] string userName)
    {
        return await _userService.VerifyUserNameExisting(userName);
    }

    [AllowAnonymous]
    [HttpGet("/user/email/existing")]
    public async Task<bool> VerifyEmailExisting([FromQuery] string email)
    {
        return await _userService.VerifyEmailExisting(email);
    }

    [AllowAnonymous]
    [HttpGet("/user/phone/existing")]
    public async Task<bool> VerifyPhoneExisting([FromQuery] string phone, [FromQuery] string regionCode = "CN")
    {
        return await _userService.VerifyPhoneExisting(phone, regionCode);
    }

    [AllowAnonymous]
    [HttpPost("/user/verifycode-out")]
    public async Task<bool> SendVerificationCodeResetPassword([FromBody] UserCreationModel user)
    {
        return await _userService.SendVerificationCodeNoLogin(user.ToUser());
    }

    [HttpPost("/user/verifycode-in")]
    public async Task<bool> SendVerificationCodeResetPasswordLogined()
    {
        return await _userService.SendVerificationCodeLogin();
    }

    [AllowAnonymous]
    [HttpPost("/user/resetpassword")]
    public async Task<bool> ResetUserPassword([FromBody] UserResetPasswordModel user)
    {
        return await _userService.ResetUserPassword(user.ToUser());
    }

    [HttpPost("/user/updatepassword")]
    public async Task<bool> UpdatePassword([FromBody] User user)
    {
        if (string.IsNullOrWhiteSpace(user.Password) || string.IsNullOrWhiteSpace(user.VerificationCode))
        {
            return false;
        }
        return await _userService.UpdatePassword(user.Password, user.VerificationCode);
    }

    [HttpPost("/user/email/modify")]
    public async Task<bool> ModifyUserEmail([FromQuery] string email)
    {
        return await _userService.ModifyUserEmail(email);
    }

    [HttpPost("/user/phone/modify")]
    public async Task<bool> ModifyUserPhone([FromQuery] string phone, [FromQuery] string regionCode = "CN")
    {
        return await _userService.ModifyUserPhone(phone, regionCode);
    }

    [HttpPost("/user/update/isdisable")]
    public async Task<bool> UpdateUsersIsDisable([FromQuery] List<string> userIds, [FromQuery] bool isDisable)
    {
        return await _userService.UpdateUsersIsDisable(userIds, isDisable);
    }

    #region User management
    [HttpPost("/users")]
    public async Task<PagedItems<UserViewModel>> GetUsers([FromBody] UserFilter filter)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var isValid = await IsValidUser();
        if (!isValid)
        {
            return new PagedItems<UserViewModel>();
        }

        var users = await userService.GetUsers(filter);
        var views = users.Items.Select(x => UserViewModel.FromUser(x)).ToList();

        return new PagedItems<UserViewModel>
        {
            Count = users.Count,
            Items = views
        };
    }

    [HttpGet("/user/{id}/details")]
    public async Task<UserViewModel> GetUserDetails(string id)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var user = await userService.GetUserDetails(id, includeAgent: true);
        return UserViewModel.FromUser(user);
    }

    [HttpPut("/user")]
    public async Task<bool> UpdateUser([FromBody] UserUpdateModel model)
    {
        if (model == null) return false;

        var isValid = await IsValidUser();
        if (!isValid)
        {
            return false;
        }

        var userService = _services.GetRequiredService<IUserService>();
        var updated = await userService.UpdateUser(UserUpdateModel.ToUser(model), isUpdateUserAgents: true);
        return updated;
    }
    #endregion


    #region Avatar
    [HttpPost("/user/avatar")]
    public bool UploadUserAvatar([FromBody] UserAvatarModel input)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var file = new FileDataModel
        {
            FileName = input.FileName,
            FileData = input.FileData,
        };
        return fileStorage.SaveUserAvatar(file);
    }

    [HttpGet("/user/avatar")]
    public IActionResult GetUserAvatar()
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var file = fileStorage.GetUserAvatar();
        if (string.IsNullOrEmpty(file))
        {
            return NoContent();
        }
        return BuildFileResult(file);
    }
    #endregion


    #region Private methods
    private async Task<bool> IsValidUser()
    {
        var userService = _services.GetRequiredService<IUserService>();
        var (isAdmin, _) = await userService.IsAdminUser(_user.Id);
        return isAdmin;
    }

    private FileContentResult BuildFileResult(string file)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var bytes = fileStorage.GetFileBytes(file);
        return File(bytes, "application/octet-stream", Path.GetFileName(file));
    }
    #endregion
}
