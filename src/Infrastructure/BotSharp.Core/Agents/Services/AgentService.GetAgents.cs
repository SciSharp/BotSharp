using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
#if !DEBUG
    [MemoryCache(10 * 60, perInstanceCache: true)]
#endif
    public async Task<PagedItems<Agent>> GetAgents(AgentFilter filter)
    {
        var agents = _db.GetAgents(filter);

        var routeSetting = _services.GetRequiredService<RoutingSettings>();
        foreach (var agent in agents)
        {
            agent.Plugin = GetPlugin(agent.Id);
        }

        agents = agents.Where(x => x.Installed).ToList();
        var pager = filter?.Pager ?? new Pagination();
        return new PagedItems<Agent>
        {
            Items = agents.Skip(pager.Offset).Take(pager.Size),
            Count = agents.Count()
        };
    }

#if !DEBUG
    [MemoryCache(10 * 60, perInstanceCache: true)]
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

        profile.Plugin = GetPlugin(profile.Id);

        return profile;
    }
}
