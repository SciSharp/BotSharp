using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Hooks;

public class RoutingAgentHook : AgentHookBase
{
    private readonly RoutingSettings _routingSetting;
    public override string SelfId => _routingSetting.AgentId;

    public RoutingAgentHook(IServiceProvider services, AgentSettings settings, RoutingSettings routingSetting) 
        : base(services, settings)
    {
        _routingSetting = routingSetting;
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        dict["router"] = _agent;

        var routing = _services.GetRequiredService<IRoutingService>();
        dict["routing_agents"] = routing.GetRoutingItems();
        dict["routing_handlers"] = routing.GetHandlers();

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
