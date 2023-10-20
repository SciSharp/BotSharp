using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;

namespace BotSharp.Core.Routing.Hooks;

public class RoutingAgentHook : AgentHookBase
{
    public RoutingAgentHook(IServiceProvider services, AgentSettings settings) 
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        dict["router"] = _agent;

        var router = _services.GetRequiredService<IRouterInstance>();
        dict["routing_agents"] = router.GetRoutingItems();
        dict["routing_handlers"] = router.GetHandlers();

        return base.OnInstructionLoaded(template, dict);
    }

    public override bool OnFunctionsLoaded(List<FunctionDef> functions)
    {
        /*functions.Add(new FunctionDef
        {
            Name = "fallback_to_router",
            Description = "If the user's request is beyond your capabilities, you can call this function for help."
        });*/
        return base.OnFunctionsLoaded(functions);
    }
}
