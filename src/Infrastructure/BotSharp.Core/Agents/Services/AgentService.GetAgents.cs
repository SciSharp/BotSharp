using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public async Task<List<Agent>> GetAgents(AgentFilter filter)
    {
        var agents = _db.GetAgents(filter);

        // Set IsRouter
        var routeSetting = _services.GetRequiredService<RoutingSettings>();
        foreach (var agent in agents)
        {
            agent.IsRouter = routeSetting.AgentIds.Contains(agent.Id);
            agent.Plugin = GetPlugin(agent.Id);
        }

        agents = agents.Where(x => x.Installed).ToList();

        return agents;
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

        // Set IsRouter
        var routeSetting = _services.GetRequiredService<RoutingSettings>();
        profile.IsRouter = routeSetting.AgentIds.Contains(profile.Id);
        profile.Plugin = GetPlugin(profile.Id);

        return profile;
    }
}
