namespace BotSharp.Abstraction.Agents;

public abstract class AgentHookBase : IAgentHook
{
    protected Agent _agent;
    public Agent Agent => _agent;

    public void SetAget(Agent agent)
    {
        _agent = agent;
    }

    public virtual bool OnAgentLoading(ref string id)
    {
        return true;
    }

    public virtual bool OnInstructionLoaded(ref string instruction)
    {
        _agent.Instruction = instruction;
        return true;
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

    public virtual Agent OnAgentLoaded()
    {
        return _agent;
    }
}
