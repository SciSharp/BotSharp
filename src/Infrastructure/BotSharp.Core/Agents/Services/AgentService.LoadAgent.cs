using BotSharp.Abstraction.Routing.Models;
using System.Collections.Concurrent;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public static ConcurrentDictionary<string, ConcurrentDictionary<string, string>> AgentParameterTypes = new();

    // [SharpCache(10, perInstanceCache: true)]
    public async Task<Agent> LoadAgent(string id, bool loadUtility = true)
    {
        if (string.IsNullOrEmpty(id) || id == Guid.Empty.ToString())
        {
            return null;
        }

        HookEmitter.Emit<IAgentHook>(_services, hook => hook.OnAgentLoading(ref id), id);

        var originalAgent = await GetAgent(id);
        var agent = originalAgent.DeepClone(modifier: agt =>
        {
            agt.ChannelInstructions = originalAgent.ChannelInstructions.DeepClone() ?? new();
            agt.Functions = originalAgent.Functions.DeepClone() ?? new();
            agt.Templates = originalAgent.Templates.DeepClone() ?? new();
            agt.Samples = originalAgent.Samples.DeepClone() ?? new();
            agt.Responses = originalAgent.Responses.DeepClone() ?? new();
            agt.LlmConfig = originalAgent.LlmConfig.DeepClone() ?? new();
            agt.Plugin = originalAgent.Plugin.DeepClone() ?? new();
        });

        if (agent == null)
        {
            return null;
        }

        agent.TemplateDict = [];
        agent.SecondaryInstructions = [];
        agent.SecondaryFunctions = [];

        await InheritAgent(agent);
        OverrideInstructionByChannel(agent);
        AddOrUpdateParameters(agent);

        // Populate state
        PopulateState(agent);

        // After agent is loaded
        HookEmitter.Emit<IAgentHook>(_services, hook => {
            hook.SetAgent(agent);

            if (!string.IsNullOrEmpty(agent.Instruction))
            {
                var dict = new Dictionary<string, object>(agent.TemplateDict);
                hook.OnInstructionLoaded(agent.Instruction, dict);
                agent.TemplateDict = new Dictionary<string, object>(dict);
            }

            if (agent.Functions != null)
            {
                hook.OnFunctionsLoaded(agent.Functions);
            }

            if (agent.Samples != null)
            {
                hook.OnSamplesLoaded(agent.Samples);
            }

            if (loadUtility && !agent.Utilities.IsNullOrEmpty())
            {
                hook.OnAgentUtilityLoaded(agent);
            }

            if (!agent.McpTools.IsNullOrEmpty())
            {
                hook.OnAgentMcpToolLoaded(agent);
            }

            hook.OnAgentLoaded(agent);

        }, id);

        _logger.LogInformation($"Loaded agent {agent}.");

        return agent;
    }

    private void OverrideInstructionByChannel(Agent agent)
    {
        var instructions = agent.ChannelInstructions;
        if (instructions.IsNullOrEmpty())
        {
            return;
        }

        var state = _services.GetRequiredService<IConversationStateService>();
        var channel = state.GetState("channel");
        
        var found = instructions.FirstOrDefault(x => x.Channel.IsEqualTo(channel));
        var defaultInstruction = instructions.FirstOrDefault(x => x.Channel == string.Empty);
        agent.Instruction = !string.IsNullOrWhiteSpace(found?.Instruction) ? found.Instruction : defaultInstruction?.Instruction;
    }

    private void PopulateState(Agent agent)
    {
        var dict = CollectRenderData(agent);
        agent.TemplateDict = new Dictionary<string, object>(dict);
    }

    private void AddOrUpdateParameters(Agent agent)
    {
        var agentId = agent.Id ?? agent.Name;
        if (AgentParameterTypes.ContainsKey(agentId))
        {
            return;
        }

        AddOrUpdateRoutesParameters(agentId, agent.RoutingRules);
        AddOrUpdateFunctionsParameters(agentId, agent.Functions);
    }

    private void AddOrUpdateRoutesParameters(string agentId, List<RoutingRule> routingRules)
    {
        var parameterTypes = AgentParameterTypes.GetOrAdd(agentId, _ => new());

        foreach (var rule in routingRules.Where(x => x.Required))
        {
            if (string.IsNullOrEmpty(rule.FieldType))
            {
                continue;
            }
            parameterTypes[rule.Field] = rule.FieldType;
        }
    }

    private void AddOrUpdateFunctionsParameters(string agentId, List<FunctionDef> functions)
    {
        var parameterTypes = AgentParameterTypes.GetOrAdd(agentId, _ => new());

        var parameters = functions.Select(p => p.Parameters);
        foreach (var param in parameters)
        {
            foreach (JsonProperty prop in param.Properties.RootElement.EnumerateObject())
            {
                var name = prop.Name;
                var node = prop.Value;
                if (node.TryGetProperty("type", out var type))
                {
                    parameterTypes[name] = type.GetString();
                }
            }
        }
    }
}
