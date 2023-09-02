using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Templating;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<Agent> LoadAgent(string id)
    {
        var hooks = _services.GetServices<IAgentHook>();

        // Before agent is loaded.
        foreach (var hook in hooks)
        {
            hook.OnAgentLoading(ref id);
        }

        var agent = await GetAgent(id);
        var templateDict = new Dictionary<string, object>();
        PopulateState(templateDict);

        // After agent is loaded
        foreach (var hook in hooks)
        {
            hook.SetAget(agent);

            if (!string.IsNullOrEmpty(agent.Instruction))
            {
                hook.OnInstructionLoaded(agent.Instruction, templateDict);
            }

            if (agent.Functions != null && agent.Functions.Any())
            {
                var functions = agent.Functions;
                hook.OnFunctionsLoaded(ref functions);
            }

            if (!string.IsNullOrEmpty(agent.Samples))
            {
                var samples = agent.Samples;
                hook.OnSamplesLoaded(ref samples);
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
        var stateService = _services.GetRequiredService<IConversationStateService>();
        var state = stateService.Load();
        foreach (var t in state)
        {
            dict[t.Key] = t.Value;
        }
    }
}
