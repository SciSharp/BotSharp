using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Roles.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public bool RefreshRoles(IEnumerable<Role> roles)
    {
        if (roles.IsNullOrEmpty()) return false;

        var validRoles = roles.Where(x => !string.IsNullOrWhiteSpace(x.Id)
                                       && !string.IsNullOrWhiteSpace(x.Name)).ToList();
        if (validRoles.IsNullOrEmpty()) return false;


        // Clear data
        _dc.RoleAgents.DeleteMany(Builders<RoleAgentDocument>.Filter.Empty);
        _dc.Roles.DeleteMany(Builders<RoleDocument>.Filter.Empty);

        var roleDocs = validRoles.Select(x => new RoleDocument
        {
            Id = x.Id,
            Name = x.Name,
            Permissions = x.Permissions,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        });
        _dc.Roles.InsertMany(roleDocs);

        return true;
    }


    public IEnumerable<Role> GetRoles(RoleFilter filter)
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

        // Search
        var roleDocs = _dc.Roles.Find(roleBuilder.And(roleFilters)).ToList();
        var roles = roleDocs.Select(x => x.ToRole()).ToList();

        return roles;
    }

    public Role? GetRoleDetails(string roleId, bool includeAgent = false)
    {
        if (string.IsNullOrWhiteSpace(roleId)) return null;

        var roleDoc = _dc.Roles.Find(Builders<RoleDocument>.Filter.Eq(x => x.Id, roleId)).FirstOrDefault();
        if (roleDoc == null) return null;

        var agentActions = new List<RoleAgentAction>();
        var role = roleDoc.ToRole();
        var roleAgentDocs = _dc.RoleAgents.Find(Builders<RoleAgentDocument>.Filter.Eq(x => x.RoleId, roleId)).ToList();

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
            var agents = GetAgents(new AgentFilter { AgentIds = agentIds });

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

    public bool UpdateRole(Role role, bool updateRoleAgents = false)
    {
        if (string.IsNullOrEmpty(role?.Id)) return false;

        var roleFilter = Builders<RoleDocument>.Filter.Eq(x => x.Id, role.Id);
        var roleUpdate = Builders<RoleDocument>.Update
                                               .Set(x => x.Name, role.Name)
                                               .Set(x => x.Permissions, role.Permissions)
                                               .Set(x => x.CreatedTime, DateTime.UtcNow)
                                               .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Roles.UpdateOne(roleFilter, roleUpdate, _options);

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

            var toDelete = _dc.RoleAgents.Find(Builders<RoleAgentDocument>.Filter.And(
                    Builders<RoleAgentDocument>.Filter.Eq(x => x.RoleId, role.Id),
                    Builders<RoleAgentDocument>.Filter.Nin(x => x.Id, roleAgentDocs.Select(x => x.Id))
                )).ToList();

            _dc.RoleAgents.DeleteMany(Builders<RoleAgentDocument>.Filter.In(x => x.Id, toDelete.Select(x => x.Id)));
            foreach (var doc in roleAgentDocs)
            {
                var roleAgentFilter = Builders<RoleAgentDocument>.Filter.Eq(x => x.Id, doc.Id);
                var roleAgentUpdate = Builders<RoleAgentDocument>.Update
                    .Set(x => x.Id, doc.Id)
                    .Set(x => x.RoleId, role.Id)
                    .Set(x => x.AgentId, doc.AgentId)
                    .Set(x => x.Actions, doc.Actions)
                    .Set(x => x.UpdatedTime, DateTime.UtcNow);

                _dc.RoleAgents.UpdateOne(roleAgentFilter, roleAgentUpdate, _options);
            }
        }

        return true;
    }
}
