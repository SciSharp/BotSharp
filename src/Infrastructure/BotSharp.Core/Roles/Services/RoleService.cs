using BotSharp.Abstraction.Users.Enums;
using System.Reflection;

namespace BotSharp.Core.Roles.Services;

public class RoleService : IRoleService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IServiceProvider services,
        ILogger<RoleService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetRoleOptions()
    {
        var fields = typeof(UserRole).GetFields(BindingFlags.Public | BindingFlags.Static)
                                     .Where(x => x.IsLiteral && !x.IsInitOnly).ToList();

        return fields.Select(x => x.GetValue(null)?.ToString())
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct()
                     .ToList();
    }

    public async Task<IEnumerable<Role>> GetRoles(RoleFilter filter)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var roles = db.GetRoles(filter);
        return roles;
    }

    public async Task<Role?> GetRoleDetails(string roleId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var role = db.GetRoleDetails(roleId);
        return role;
    }

    public async Task<bool> UpdateRole(Role role, bool isUpdateRoleAgents = false)
    {
        if (role == null) return false;

        if (string.IsNullOrEmpty(role.Id))
        {
            role.Id = Guid.NewGuid().ToString();
        }
        
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.UpdateRole(role, isUpdateRoleAgents);
    }
}
