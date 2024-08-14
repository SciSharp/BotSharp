using BotSharp.Abstraction.Routing.Models;
using System.Collections.Concurrent;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public static ConcurrentDictionary<string, Dictionary<string,string>> AgentParameterTypes = new();

    [MemoryCache(10 * 60, perInstanceCache: true)]
    public async Task<Agent> LoadAgent(string id)
    {
        if (string.IsNullOrEmpty(id) || id == Guid.Empty.ToString())
        {
            return null;
        }

        var hooks = _services.GetServices<IAgentHook>();

        // Before agent is loaded.
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != id)
            {
                continue;
            }

            hook.OnAgentLoading(ref id);
        }

        var agent = await GetAgent(id);
        if (agent == null)
        {
            return null;
        }

        await InheritAgent(agent);
        OverrideInstructionByChannel(agent);
        AddOrUpdateParameters(agent);

        // Populate state into dictionary
        agent.TemplateDict = new Dictionary<string, object>();
        PopulateState(agent.TemplateDict);

        // After agent is loaded
        foreach (var hook in hooks)
        {
            if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != id)
            {
                continue;
            }

            hook.SetAget(agent);

            if (!string.IsNullOrEmpty(agent.Instruction))
            {
                hook.OnInstructionLoaded(agent.Instruction, agent.TemplateDict);
            }

            if (agent.Functions != null)
            {
                hook.OnFunctionsLoaded(agent.Functions);
            }

            if (agent.Samples != null)
            {
                hook.OnSamplesLoaded(agent.Samples);
            }

            hook.OnAgentLoaded(agent);
        }

        _logger.LogInformation($"Loaded agent {agent}.");

        return agent;
    }

    private void OverrideInstructionByChannel(Agent agent)
    {
        var instructions = agent.ChannelInstructions;
        if (instructions.IsNullOrEmpty()) return;

        var state = _services.GetRequiredService<IConversationStateService>();
        var channel = state.GetState("channel");

        if (string.IsNullOrWhiteSpace(channel))
        {
            return;
        }

        var found = instructions.FirstOrDefault(x => x.Channel.IsEqualTo(channel));
        agent.Instruction = !string.IsNullOrWhiteSpace(found?.Instruction) ? found.Instruction : agent.Instruction;
    }

    private void PopulateState(Dictionary<string, object> dict)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        foreach (var t in conv.States.GetStates())
        {
            dict[t.Key] = t.Value;
        }
    }

    private void AddOrUpdateParameters(Agent agent)
    {
        var agentId = agent.Id ?? agent.Name;
        if (AgentParameterTypes.ContainsKey(agentId)) return;
        
        AddOrUpdateRoutesParameters(agentId, agent.RoutingRules);
        AddOrUpdateFunctionsParameters(agentId, agent.Functions);
    }

    private void AddOrUpdateRoutesParameters(string agentId, List<RoutingRule> routingRules)
    {
        if(!AgentParameterTypes.TryGetValue(agentId, out var parameterTypes))
        {
            parameterTypes = new();
        }

        foreach (var rule in routingRules.Where(x => x.Required))
        {
            if (string.IsNullOrEmpty(rule.FieldType)) continue;
            parameterTypes.TryAdd(rule.Field, rule.FieldType);
        }

        AgentParameterTypes.TryAdd(agentId, parameterTypes);
    }

    private void AddOrUpdateFunctionsParameters(string agentId, List<FunctionDef> functions)
    {
        if (!AgentParameterTypes.TryGetValue(agentId, out var parameterTypes))
        {
            parameterTypes = new();
        }

        var parameters = functions.Select(p => p.Parameters);
        foreach (var param in parameters)
        {
            foreach (JsonProperty prop in param.Properties.RootElement.EnumerateObject())
            {
                var name = prop.Name;
                var node = prop.Value;
                if (node.TryGetProperty("type", out var type))
                {
                    parameterTypes.TryAdd(name, type.GetString());
                }
            }
        }

        AgentParameterTypes.TryAdd(agentId, parameterTypes);
    }
}
