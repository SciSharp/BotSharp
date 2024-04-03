using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
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
            throw new Exception($"Can't load agent by id: {id}");
        }

        if (agent.InheritAgentId != null)
        {
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

            if (agent.Instruction == null)
            {
                agent.Instruction = inheritedAgent.Instruction;
            }
        }

        agent.TemplateDict = new Dictionary<string, object>();

        // Populate state into dictionary
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

    private void PopulateState(Dictionary<string, object> dict)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        foreach (var t in conv.States.GetStates())
        {
            dict[t.Key] = t.Value;
        }
    }
}
