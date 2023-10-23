using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public async Task<List<Agent>> GetAgents(bool? allowRouting = null)
    {
        var agents = _db.GetAgents(allowRouting: allowRouting);
        return await Task.FromResult(agents);
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
