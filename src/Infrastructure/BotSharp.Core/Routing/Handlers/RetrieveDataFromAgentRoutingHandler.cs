using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Core.Routing.Planning;

namespace BotSharp.Core.Routing.Handlers;

/// <summary>
/// Retrieve information from specific agent
/// </summary>
public class RetrieveDataFromAgentRoutingHandler : RoutingHandlerBase//, IRoutingHandler
{
    public string Name => "retrieve_data_from_agent";

    public string Description => "Retrieve data from appropriate agent.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", "why choose this function"),
        new ParameterPropertyDef("question", "the question you will ask the next action agent to get the necessary information"),
        new ParameterPropertyDef("next_action_agent", "agent that can handle the question"),
        new ParameterPropertyDef("user_goal_agent", "agent that can achieve user original goal"),
        new ParameterPropertyDef("args", "required parameters extracted from question and hand over to the next agent")
        {
            Type = "object"
        }
    };

    public List<string> Planers => new List<string>
    {
        nameof(HFPlanner)
    };

    public RetrieveDataFromAgentRoutingHandler(IServiceProvider services, ILogger<RetrieveDataFromAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var context = _services.GetRequiredService<IRoutingContext>();
        var agentId = context.GetCurrentAgentId();
        var dialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, inst.Question)
            {
                CurrentAgentId = agentId,
                MessageId = message.MessageId
            }
        };

        var ret = await routing.InvokeAgent(agentId, dialogs, onFunctionExecuting);
        var response = dialogs.Last();
        inst.Response = response.Content;

        // Add final response to parent dialog
        _dialogs.Add(response);

        return ret;
    }
}
