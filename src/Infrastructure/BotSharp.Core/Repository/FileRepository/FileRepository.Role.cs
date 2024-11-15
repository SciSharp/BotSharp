using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public bool RefreshRoles(IEnumerable<Role> roles)
    {
        if (roles.IsNullOrEmpty()) return false;

        var validRoles = roles.Where(x => !string.IsNullOrWhiteSpace(x.Id)
                                       && !string.IsNullOrWhiteSpace(x.Name)).ToList();
        if (validRoles.IsNullOrEmpty()) return false;

        var baseDir = Path.Combine(_dbSettings.FileRepository, ROLES_FOLDER);
        if (Directory.Exists(baseDir))
        {
            Directory.Delete(baseDir, true);
        }

        Directory.CreateDirectory(baseDir);

        foreach (var role in validRoles)
        {
            var dir = Path.Combine(baseDir, role.Id);
            Directory.CreateDirectory(dir);
            Thread.Sleep(50);
            var roleFile = Path.Combine(dir, ROLE_FILE);
            role.CreatedTime = DateTime.UtcNow;
            role.UpdatedTime = DateTime.UtcNow;
            File.WriteAllText(roleFile, JsonSerializer.Serialize(role, _options));
        }

        return true;
    }

    public IEnumerable<Role> GetRoles(RoleFilter filter)
    {
        var roles = Roles;
        if (filter == null)
        {
            filter = RoleFilter.Empty();
        }

        // Apply filters
        if (!filter.Names.IsNullOrEmpty())
        {
            roles = roles.Where(x => filter.Names.Contains(x.Id));
        }

        return roles.ToList();
    }

    public Role? GetRoleDetails(string roleId, bool includeAgent = false)
    {
        if (string.IsNullOrWhiteSpace(roleId)) return null;

        var role = Roles.FirstOrDefault(x => x.Id == roleId);
        if (role == null) return null;

        var agentActions = new List<RoleAgentAction>();
        var roleAgents = RoleAgents?.Where(x => x.RoleId == roleId)?.ToList() ?? [];

        if (!includeAgent)
        {
            agentActions = roleAgents.Select(x => new RoleAgentAction
            {
                Id = x.Id,
                AgentId = x.AgentId,
                Actions = x.Actions
            }).ToList();
            role.AgentActions = agentActions;
            return role;
        }
        
        var agentIds = roleAgents.Select(x => x.AgentId).Distinct().ToList();
        if (!agentIds.IsNullOrEmpty())
        { 
            var agents = GetAgents(new AgentFilter { AgentIds = agentIds });

            foreach (var item in roleAgents)
            {
                var found = agents.FirstOrDefault(x => x.Id == item.AgentId);
                if (found == null) continue;

                agentActions.Add(new RoleAgentAction
                {
                    Id = item.Id,
                    AgentId = found.Id,
                    Agent = found,
                    Actions = item.Actions
                });
            }
        }

        role.AgentActions = agentActions;
        return role;
    }

    public bool UpdateRole(Role role, bool updateRoleAgents = false)
    {
        if (string.IsNullOrEmpty(role?.Id) || string.IsNullOrEmpty(role?.Name))
        {
            return false;
        }

        var dir = Path.Combine(_dbSettings.FileRepository, ROLES_FOLDER, role.Id);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var roleFile = Path.Combine(dir, ROLE_FILE);
        role.CreatedTime = DateTime.UtcNow;
        role.UpdatedTime = DateTime.UtcNow;
        File.WriteAllText(roleFile, JsonSerializer.Serialize(role, _options));

        if (updateRoleAgents)
        {
            var roleAgents = role.AgentActions?.Select(x => new RoleAgent
            {
                Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
                RoleId = role.Id,
                AgentId = x.AgentId,
                Actions = x.Actions ?? [],
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            })?.ToList() ?? [];

            var roleAgentFile = Path.Combine(dir, ROLE_AGENT_FILE);
            File.WriteAllText(roleAgentFile, JsonSerializer.Serialize(roleAgents, _options));
            _roleAgents = [];
        }

        _roles = [];
        return true;
    }
}
