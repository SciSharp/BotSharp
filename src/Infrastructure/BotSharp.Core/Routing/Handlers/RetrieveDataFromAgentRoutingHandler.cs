using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Core.Planning;

namespace BotSharp.Core.Routing.Handlers;

/// <summary>
/// Retrieve information from specific agent
/// </summary>
public class RetrieveDataFromAgentRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "retrieve_data_from_agent";

    public string Description => "Retrieve data from appropriate agent.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", "why retrieve data"),
        new ParameterPropertyDef("question", "the question you will ask the agent to get the necessary data"),
        new ParameterPropertyDef("next_action_agent", "agent that can handle the question"),
        new ParameterPropertyDef("args", "required parameters extracted from question and hand over to the next agent")
        {
            Type = "object"
        }
    };

    public List<string> Planers => new List<string>
    {
        nameof(ReasoningPlanner)
    };

    public RetrieveDataFromAgentRoutingHandler(IServiceProvider services, ILogger<RetrieveDataFromAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var context = _services.GetRequiredService<RoutingContext>();
        var ret = await routing.InvokeAgent(context.GetCurrentAgentId(), message);

        return ret;
    }
}
