using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Hooks;

public class RoutingAgentHook : AgentHookBase
{
    private readonly RoutingSettings _routingSetting;
    public override string SelfId => string.Empty;

    public RoutingAgentHook(IServiceProvider services, AgentSettings settings, RoutingSettings routingSetting) 
        : base(services, settings)
    {
        _routingSetting = routingSetting;
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        if (_agent.Type != AgentType.Routing)
        {
            return base.OnInstructionLoaded(template, dict);
        }
        dict["router"] = _agent;

        var routing = _services.GetRequiredService<IRoutingService>();
        var agents = routing.GetRoutableAgents(_agent.Profiles);
        dict["routing_agents"] = agents;
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
