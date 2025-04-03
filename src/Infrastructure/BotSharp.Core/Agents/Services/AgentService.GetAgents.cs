using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
#if !DEBUG
    [SharpCache(10)]
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
    [SharpCache(10)]
#endif
    public async Task<List<IdName>> GetAgentOptions(List<string>? agentIds)
    {
        var agents = _db.GetAgents(new AgentFilter
        {
            AgentIds = !agentIds.IsNullOrEmpty() ? agentIds : null
        });
        return agents?.Select(x => new IdName(x.Id, x.Name))?.OrderBy(x => x.Name)?.ToList() ?? [];
    }

#if !DEBUG
    [SharpCache(10)]
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
        else if (string.IsNullOrEmpty(profile.LlmConfig?.Provider) || string.IsNullOrEmpty(profile.LlmConfig?.Model))
        {
            profile.LlmConfig.Provider = agentSetting.LlmConfig.Provider;
            profile.LlmConfig.Model = agentSetting.LlmConfig.Model;
        }

        profile.Plugin = GetPlugin(profile.Id);
        return profile;
    }

    public async Task InheritAgent(Agent agent)
    {
        if (string.IsNullOrWhiteSpace(agent?.InheritAgentId)) return;

        var inheritedAgent = await GetAgent(agent.InheritAgentId);
        agent.Templates.AddRange(inheritedAgent.Templates
            // exclude private template
            .Where(x => !x.Name.StartsWith("."))
            // exclude duplicate name
            .Where(x => !agent.Templates.Exists(t => t.Name == x.Name)));

        agent.Functions.AddRange(inheritedAgent.Functions
            // exclude private template
            .Where(x => !x.Name.StartsWith("."))
            // exclude duplicate name
            .Where(x => !agent.Functions.Exists(t => t.Name == x.Name)));

        if (string.IsNullOrWhiteSpace(agent.Instruction))
        {
            agent.Instruction = inheritedAgent.Instruction;
        }
    }
}
