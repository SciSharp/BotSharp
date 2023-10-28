using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class RouteToAgentRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "route_to_agent";

    public string Description => "Route request to appropriate agent.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", "why route to agent"),
        new ParameterPropertyDef("next_action_agent", "agent for next action based on user latest response"),
        new ParameterPropertyDef("user_goal_agent", "agent who can achieve user original goal"),
        new ParameterPropertyDef("args", "useful parameters of next action agent, format: { }")
        {
            Type = "object"
        }
    };

    public RouteToAgentRoutingHandler(IServiceProvider services, ILogger<RouteToAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var context = _services.GetRequiredService<RoutingContext>();
        var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == inst.Function);
        message.FunctionArgs = JsonSerializer.Serialize(inst);
        var ret = await function.Execute(message);

        ret = await routing.InvokeAgent(context.GetCurrentAgentId(), message);

        return true;
    }
}
