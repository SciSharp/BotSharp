using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.Services;

public abstract class AgentHookBase : IAgentHook
{
    protected Agent _agent;
    public Agent Agent => _agent;
    
    protected readonly IServiceProvider _services;
    protected readonly AgentSettings _settings;

    public AgentHookBase(IServiceProvider services, AgentSettings settings)
    {
        _services = services;
        _settings = settings;
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
        return true;
    }

    public virtual bool OnFunctionsLoaded(ref List<string> functions)
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
