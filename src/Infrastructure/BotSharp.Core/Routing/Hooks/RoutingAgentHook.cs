using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Core.Routing.Hooks;

public class RoutingAgentHook : AgentHookBase
{
    public RoutingAgentHook(IServiceProvider services, AgentSettings settings) 
        : base(services, settings)
    {
    }

    public override bool OnFunctionsLoaded(ref List<FunctionDef> functions)
    {
        functions.Add(new FunctionDef
        {
            Name = "fallback_to_router",
            Description = "If the user's request is beyond your capabilities, you can call this function for help."
        });
        return base.OnFunctionsLoaded(ref functions);
    }
}
