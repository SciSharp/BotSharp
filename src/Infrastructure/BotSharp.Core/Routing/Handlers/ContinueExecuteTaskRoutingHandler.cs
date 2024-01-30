using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Core.Routing.Planning;

namespace BotSharp.Core.Routing.Handlers;

public class ContinueExecuteTaskRoutingHandler : RoutingHandlerBase//, IRoutingHandler
{
    public string Name => "continue_execute_task";

    public string Description => "Continue to execute user's request without further information retrival.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("next_action_agent", "agent for next action based on user latest response"),
        new ParameterPropertyDef("user_goal_agent", "agent who can achieve user original goal"),
        new ParameterPropertyDef("reason", "why continue to execute current task"),
        new ParameterPropertyDef("args", "required parameters extracted from question")
        {
            Type = "object"
        }
    };

    public List<string> Planers => new List<string>
    {
        nameof(HFPlanner)
    };

    public ContinueExecuteTaskRoutingHandler(IServiceProvider services, ILogger<ContinueExecuteTaskRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var filter = new AgentFilter { AgentName = inst.AgentName };
        var record = db.GetAgents(filter).FirstOrDefault();

        message.FunctionName = inst.Function;
        message.CurrentAgentId = record.Id;
        message.FunctionArgs = JsonSerializer.Serialize(inst.Arguments);

        return true;
    }
}
