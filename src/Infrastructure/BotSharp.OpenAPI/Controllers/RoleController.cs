using BotSharp.Abstraction.Roles;
using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class RoleController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IRoleService _roleService;
    private readonly IUserIdentity _user;

    public RoleController(
        IServiceProvider services,
        IRoleService roleService,
        IUserIdentity user)
    {
        _services = services;
        _roleService = roleService;
        _user = user;
    }

    [HttpPost("/role/refresh")]
    public async Task<bool> RefreshRoles()
    {
        var isValid = await IsValidUser();
        if (!isValid)
        {
            return false;
        }

        return await _roleService.RefreshRoles();
    }


    [HttpGet("/role/options")]
    public async Task<IEnumerable<string>> GetRoleOptions()
    {
        return await _roleService.GetRoleOptions();
    }

    [HttpPost("/roles")]
    public async Task<IEnumerable<RoleViewModel>> GetRoles([FromBody] RoleFilter? filter = null)
    {
        if (filter == null)
        {
            filter = RoleFilter.Empty();
        }

        var isValid = await IsValidUser();
        if (!isValid)
        {
            return Enumerable.Empty<RoleViewModel>();
        }

        var roles = await _roleService.GetRoles(filter);
        return roles.Select(x => RoleViewModel.FromRole(x)).ToList();
    }

    [HttpGet("/role/{id}/details")]
    public async Task<RoleViewModel> GetRoleDetails([FromRoute] string id)
    {
        var role = await _roleService.GetRoleDetails(id, includeAgent: true);
        return RoleViewModel.FromRole(role);
    }

    [HttpPut("/role")]
    public async Task<bool> UpdateRole([FromBody] RoleUpdateModel model)
    {
        if (model == null) return false;

        var isValid = await IsValidUser();
        if (!isValid)
        {
            return false;
        }

        var role = RoleUpdateModel.ToRole(model);
        return await _roleService.UpdateRole(role, isUpdateRoleAgents: true);
    }

    private async Task<bool> IsValidUser()
    {
        var userService = _services.GetRequiredService<IUserService>();
        return await userService.IsAdminUser(_user.Id);
    }
}
