using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    [SharpCache(10)]
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

    [SharpCache(10)]
    public async Task<List<IdName>> GetAgentOptions(List<string>? agentIdsOrNames, bool byName = false)
    {
        var agents = byName ? 
            _db.GetAgents(new AgentFilter
            {
                AgentNames = !agentIdsOrNames.IsNullOrEmpty() ? agentIdsOrNames : null
            }) :
            _db.GetAgents(new AgentFilter
            {
                AgentIds = !agentIdsOrNames.IsNullOrEmpty() ? agentIdsOrNames : null
            });

        return agents?.Select(x => new IdName(x.Id, x.Name))?.OrderBy(x => x.Name)?.ToList() ?? [];
    }

    [SharpCache(10)]
    public async Task<Agent> GetAgent(string id)
    {
         if (string.IsNullOrWhiteSpace(id))
         {
             return null;
         }

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

        AddDefaultInstruction(profile, profile.Instruction);

        return profile;
    }

    /// <summary>
    /// Add default instruction to ChannelInstructions
    /// </summary>
    private void AddDefaultInstruction(Agent agent, string instruction)
    {
        //check if instruction is empty
        if (string.IsNullOrWhiteSpace(instruction)) return;
        //check if instruction is already set
        if (agent.ChannelInstructions.Exists(p => p.Channel == string.Empty)) return;
        //Add default instruction to ChannelInstructions
        var defaultInstruction = new ChannelInstruction() { Channel = string.Empty, Instruction = instruction };
        agent.ChannelInstructions.Insert(0, defaultInstruction);
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
            AddDefaultInstruction(agent, inheritedAgent.Instruction);
        }
    }
}
