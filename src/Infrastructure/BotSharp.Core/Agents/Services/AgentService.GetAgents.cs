using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories.Filters;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public async Task<List<Agent>> GetAgents(AgentFilter filter)
    {
        var agents = _db.GetAgents(filter);
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
        }

        // Load llm config
        var agentSetting = _services.GetRequiredService<AgentSettings>();
        if (profile.LlmConfig == null)
        {
            profile.LlmConfig = agentSetting.LlmConfig;
            profile.LlmConfig.IsInherit = true;
        }

        return profile;
    }
}
