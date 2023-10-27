using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class RouteToAgentRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "route_to_agent";

    public string Description => "Route request to appropriate agent.";

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("reason", "why route to agent"),
        new NameDesc("next_action_agent", "agent for next action based on user latest response"),
        new NameDesc("user_goal_agent", "agent who can achieve user original goal"),
        new NameDesc("args", "useful parameters of next action agent")
    };

    public bool IsReasoning => false;

    public RouteToAgentRoutingHandler(IServiceProvider services, ILogger<RouteToAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(IRoutingService routing, FunctionCallFromLlm inst)
    {
        var context = _services.GetRequiredService<RoutingContext>();

        var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == inst.Function);
        var message = new RoleDialogModel(AgentRole.Function, inst.Question)
        {
            MessageId = inst.MessageId,
            FunctionName = inst.Function,
            FunctionArgs = JsonSerializer.Serialize(inst),
            CurrentAgentId = context.GetCurrentAgentId(),
        };

        var ret = await function.Execute(message);

        var result = await routing.InvokeAgent(context.GetCurrentAgentId());
        // Keep last message data for debug
        result.Data = result.Data ?? message.Data;
        result.FunctionName = result.FunctionName ?? message.FunctionName;

        return result;
    }
}
