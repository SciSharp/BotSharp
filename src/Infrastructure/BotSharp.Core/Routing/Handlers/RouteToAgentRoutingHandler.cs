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
        new NameDesc("agent", "the name of the agent"),
        new NameDesc("reason", "why route to this agent"),
        new NameDesc("args", "the agent required parameters")
    };

    public bool IsReasoning => false;

    public RouteToAgentRoutingHandler(IServiceProvider services, ILogger<RouteToAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(IRoutingService routing, FunctionCallFromLlm inst)
    {
        var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == inst.Function);
        var message = new RoleDialogModel(AgentRole.Function, inst.Question)
        {
            FunctionName = inst.Function,
            FunctionArgs = JsonSerializer.Serialize(new RoutingArgs
            {
                AgentName = inst.AgentName
            }),
            CurrentAgentId = routing.Dialogs.Last().CurrentAgentId
        };

        var ret = await function.Execute(message);

        var context = _services.GetRequiredService<RoutingContext>();
        var result = await routing.InvokeAgent(context.CurrentAgentId);
        
        return result;
    }
}
