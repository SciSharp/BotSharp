using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    [MemoryCache(10 * 60, perInstanceCache: true)]
    public async Task<Agent> LoadAgent(string id)
    {
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

        var templateDict = new Dictionary<string, object>();
        PopulateState(templateDict);

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
                hook.OnInstructionLoaded(agent.Instruction, templateDict);
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

        // render liquid template
        var render = _services.GetRequiredService<ITemplateRender>();
        agent.Instruction = render.Render(agent.Instruction, templateDict);

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
