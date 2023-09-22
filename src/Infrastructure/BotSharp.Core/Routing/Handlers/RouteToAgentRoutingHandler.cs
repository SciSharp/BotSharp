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

    public List<string> Parameters => new List<string>
    {
        "1. agent_name: the name of the agent",
        "2. reason: why route to this agent",
        "3. args: parameters extracted from context",
        "4. answer: if you know how to response without asking to other agent",
        "5. goal: user's original goal"
    };

    public bool IsReasoning => false;

    public RouteToAgentRoutingHandler(IServiceProvider services, ILogger<RouteToAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
    {
        if (string.IsNullOrEmpty(inst.Route.AgentName))
        {
            inst = await GetNextInstructionFromReasoner($"What's the next step? your response must have agent name.");
        }

        var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == inst.Function);
        var message = new RoleDialogModel(AgentRole.Function, inst.Question)
        {
            FunctionName = inst.Function,
            FunctionArgs = JsonSerializer.Serialize(new RoutingArgs
            {
                AgentName = inst.Route.AgentName
            }),
        };

        var ret = await function.Execute(message);

        var result = await InvokeAgent(message.CurrentAgentId, _dialogs);
        result.ExecutionData = result.ExecutionData ?? message.ExecutionData;

        if (result.Role == AgentRole.Function && !result.StopCompletion)
        {
            _dialogs.Add(result);
            result = await InvokeAgent(message.CurrentAgentId, _dialogs);
        }
        
        return result;
    }
}
