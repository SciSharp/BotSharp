using BotSharp.Abstraction.Agents.Models;
using Fluid;

namespace BotSharp.Core.Agents.Services;

public abstract class AgentHookBase : IAgentHook
{
    protected Agent _agent;
    public Agent Agent => _agent;
    private static readonly FluidParser _parser = new FluidParser();
    
    private readonly IServiceProvider _services;

    public AgentHookBase(IServiceProvider services)
    {
        _services = services;
    }

    public void SetAget(Agent agent)
    {
        _agent = agent;
    }

    public virtual bool OnAgentLoading(ref string id)
    {
        return true;
    }

    public virtual bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        if (_parser.TryParse(template, out var t, out var error))
        {
            PopulateStateTokens(dict);
            var context = new TemplateContext(dict);
            _agent.Instruction = t.Render(context);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void PopulateStateTokens(Dictionary<string, object> dict)
    {
        var stateService = _services.GetRequiredService<IConversationStateService>();
        var state = stateService.Load();
        foreach (var t in state)
        {
            dict[t.Key] = t.Value;
        }
    }

    public virtual bool OnFunctionsLoaded(ref string functions)
    {
        _agent.Functions = functions;
        return true;
    }

    public virtual bool OnSamplesLoaded(ref string samples)
    {
        _agent.Samples = samples;
        return true;
    }

    public virtual void  OnAgentLoaded(Agent agent)
    {
    }
}
