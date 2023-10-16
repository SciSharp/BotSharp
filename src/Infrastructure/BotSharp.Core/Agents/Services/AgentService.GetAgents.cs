using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public async Task<List<Agent>> GetAgents()
    {
        var query = from a in _db.Agents
                    join ua in _db.UserAgents on a.Id equals ua.AgentId
                    join u in _db.Users on ua.UserId equals u.Id
                    where ua.UserId == _user.Id || u.ExternalId == _user.Id || a.IsPublic
                    select a;
        return query.ToList();
    }

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public async Task<Agent> GetAgent(string id)
    {
        var profile = _db.GetAgent(id);

        if (profile == null)
        {
            _logger.LogError($"Can't find agent {id}");
            return null;
        };

        return profile;
    }
}
