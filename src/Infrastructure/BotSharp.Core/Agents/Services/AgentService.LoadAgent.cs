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

    public string RenderedTemplate(Agent agent, string templateName)
    {
        // render liquid template
        var render = _services.GetRequiredService<ITemplateRender>();
        var template = agent.Templates.First(x => x.Name == templateName).Content;
        return render.Render(template, agent.TemplateDict);
    }

    public string RenderedInstruction(Agent agent)
    {
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(agent.Instruction, agent.TemplateDict);
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
