using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Roles.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public async Task<bool> RefreshRoles(IEnumerable<Role> roles)
    {
        if (roles.IsNullOrEmpty()) return false;

        var validRoles = roles.Where(x => !string.IsNullOrWhiteSpace(x.Id)
                                       && !string.IsNullOrWhiteSpace(x.Name)).ToList();
        if (validRoles.IsNullOrEmpty()) return false;


        // Clear data
        await _dc.RoleAgents.DeleteManyAsync(Builders<RoleAgentDocument>.Filter.Empty);
        await _dc.Roles.DeleteManyAsync(Builders<RoleDocument>.Filter.Empty);

        var roleDocs = validRoles.Select(x => new RoleDocument
        {
            Id = x.Id,
            Name = x.Name,
            Permissions = x.Permissions,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        });
        await _dc.Roles.InsertManyAsync(roleDocs);

        return true;
    }


    public async Task<IEnumerable<Role>> GetRoles(RoleFilter filter)
    {
        if (filter == null)
        {
            filter = RoleFilter.Empty();
        }

        var roleBuilder = Builders<RoleDocument>.Filter;
        var roleFilters = new List<FilterDefinition<RoleDocument>>() { roleBuilder.Empty };

        // Apply filters
        if (!filter.Names.IsNullOrEmpty())
        {
            roleFilters.Add(roleBuilder.In(x => x.Name, filter.Names));
        }

        if (!filter.ExcludeRoles.IsNullOrEmpty())
        {
            roleFilters.Add(roleBuilder.Nin(x => x.Name, filter.ExcludeRoles));
        }

        // Search
        var roleDocs = await _dc.Roles.Find(roleBuilder.And(roleFilters)).ToListAsync();
        var roles = roleDocs.Select(x => x.ToRole()).ToList();

        return roles;
    }

    public async Task<Role?> GetRoleDetails(string roleId, bool includeAgent = false)
    {
        if (string.IsNullOrWhiteSpace(roleId)) return null;

        var roleDoc = await _dc.Roles.Find(Builders<RoleDocument>.Filter.Eq(x => x.Id, roleId)).FirstOrDefaultAsync();
        if (roleDoc == null) return null;

        var agentActions = new List<RoleAgentAction>();
        var role = roleDoc.ToRole();
        var roleAgentDocs = await _dc.RoleAgents.Find(Builders<RoleAgentDocument>.Filter.Eq(x => x.RoleId, roleId)).ToListAsync();

        if (!includeAgent)
        {
            agentActions = roleAgentDocs.Select(x => new RoleAgentAction
            {
                Id = x.Id,
                AgentId = x.AgentId,
                Actions = x.Actions
            }).ToList();
            role.AgentActions = agentActions;
            return role;
        }

        var agentIds = roleAgentDocs.Select(x => x.AgentId).Distinct().ToList();
        if (!agentIds.IsNullOrEmpty())
        {
            var agents = await GetAgents(new AgentFilter { AgentIds = agentIds });

            foreach (var item in roleAgentDocs)
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

    public async Task<bool> UpdateRole(Role role, bool updateRoleAgents = false)
    {
        if (string.IsNullOrEmpty(role?.Id)) return false;

        var roleFilter = Builders<RoleDocument>.Filter.Eq(x => x.Id, role.Id);
        var roleUpdate = Builders<RoleDocument>.Update
                                               .Set(x => x.Name, role.Name)
                                               .Set(x => x.Permissions, role.Permissions)
                                               .Set(x => x.CreatedTime, DateTime.UtcNow)
                                               .Set(x => x.UpdatedTime, DateTime.UtcNow);

        await _dc.Roles.UpdateOneAsync(roleFilter, roleUpdate, _options);

        if (updateRoleAgents)
        {
            var roleAgentDocs = role.AgentActions?.Select(x => new RoleAgentDocument
            {
                Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
                RoleId = role.Id,
                AgentId = x.AgentId,
                Actions = x.Actions,
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            })?.ToList() ?? [];

            var toDelete = await _dc.RoleAgents.Find(Builders<RoleAgentDocument>.Filter.And(
                    Builders<RoleAgentDocument>.Filter.Eq(x => x.RoleId, role.Id),
                    Builders<RoleAgentDocument>.Filter.Nin(x => x.Id, roleAgentDocs.Select(x => x.Id))
                )).ToListAsync();

            await _dc.RoleAgents.DeleteManyAsync(Builders<RoleAgentDocument>.Filter.In(x => x.Id, toDelete.Select(x => x.Id)));
            foreach (var doc in roleAgentDocs)
            {
                var roleAgentFilter = Builders<RoleAgentDocument>.Filter.Eq(x => x.Id, doc.Id);
                var roleAgentUpdate = Builders<RoleAgentDocument>.Update
                    .Set(x => x.Id, doc.Id)
                    .Set(x => x.RoleId, role.Id)
                    .Set(x => x.AgentId, doc.AgentId)
                    .Set(x => x.Actions, doc.Actions)
                    .Set(x => x.UpdatedTime, DateTime.UtcNow);

                await _dc.RoleAgents.UpdateOneAsync(roleAgentFilter, roleAgentUpdate, _options);
            }
        }

        return true;
    }
}
