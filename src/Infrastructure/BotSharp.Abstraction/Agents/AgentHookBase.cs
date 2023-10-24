using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Agents;

public abstract class AgentHookBase : IAgentHook
{
    public virtual string SelfId => throw new NotImplementedException("Please set SelfId as agent id!");

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
        dict["current_date"] = $"{DateTime.Now:MMM dd, yyyy}";
        dict["current_time"] = $"{DateTime.Now:hh:mm tt}";
        dict["current_weekday"] = $"{DateTime.Now:dddd}";
        return true;
    }

    public virtual bool OnFunctionsLoaded(List<FunctionDef> functions)
    {
        _agent.Functions = functions;
        return true;
    }

    public virtual bool OnSamplesLoaded(List<string> samples)
    {
        _agent.Samples = samples;
        return true;
    }

    public virtual void  OnAgentLoaded(Agent agent)
    {
    }
}
