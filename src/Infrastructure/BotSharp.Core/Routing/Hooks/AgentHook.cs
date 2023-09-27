using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Core.Routing.Hooks;

public class AgentHook : AgentHookBase
{
    public AgentHook(IServiceProvider services, AgentSettings settings) 
        : base(services, settings)
    {
    }

    public override bool OnFunctionsLoaded(ref List<FunctionDef> functions)
    {
        return base.OnFunctionsLoaded(ref functions);
    }
}
