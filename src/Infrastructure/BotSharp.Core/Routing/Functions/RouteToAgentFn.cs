using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Core.Routing;

/// <summary>
/// Router calls this function to set the Active Agent according to the context
/// </summary>
public partial class RouteToAgentFn : IFunctionCallback
{
    public string Name => "route_to_agent";
    private readonly IServiceProvider _services;
    private readonly IRoutingContext _context;

    public RouteToAgentFn(IServiceProvider services, IRoutingContext context)
    {
        _services = services;
        _context = context;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<RoutingArgs>(message.FunctionArgs);
        var states = _services.GetRequiredService<IConversationStateService>();

        // Push original task agent
        if (!string.IsNullOrEmpty(args.OriginalAgent) && args.OriginalAgent.Length < 32)
        {
            // Correct user goal agent to keep orignal task
            var goalAgentInState = states.GetState("user_goal_agent", string.Empty);
            bool correctToOriginalAgent = false;
            if (goalAgentInState == string.Empty)
            {
                states.SetState("user_goal_agent", args.OriginalAgent, isNeedVersion: true);
            }
            else if (args.OriginalAgent == args.AgentName && args.OriginalAgent != goalAgentInState)
            {
                // Correct to original agent
                args.OriginalAgent = goalAgentInState;
                correctToOriginalAgent = true;
            }
            else if (args.OriginalAgent != args.AgentName && args.OriginalAgent != goalAgentInState)
            {
                // Correct to original agent
                states.SetState("user_goal_agent", args.OriginalAgent, isNeedVersion: true);
            }

            var db = _services.GetRequiredService<IBotSharpRepository>();
            var filter = new AgentFilter { AgentName = args.OriginalAgent };
            var originalAgent = db.GetAgents(filter).FirstOrDefault();
            if (originalAgent != null)
            {
                _context.Push(originalAgent.Id, $"user goal agent{(correctToOriginalAgent ? " & is corrected" : "")}");
            }
        }

        // Push next action agent
        if (!string.IsNullOrEmpty(args.AgentName) && args.AgentName.Length < 32)
        {
            _context.Push(args.AgentName, args.NextActionReason);
            states.SetState(StateConst.NEXT_ACTION_AGENT, args.AgentName, isNeedVersion: true);
        }

        if (string.IsNullOrEmpty(args.AgentName))
        {
            message.Content = $"missing agent name";
        }
        else
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var filter = new AgentFilter { AgentName = args.AgentName };
            var targetAgent = db.GetAgents(filter).FirstOrDefault();
            if (targetAgent == null)
            {
                message.Data = JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs);
                return false;
            }

            if (targetAgent.Disabled)
            {
                return false;
            }

            var routing = _services.GetRequiredService<IRoutingService>();
            var missingfield = routing.HasMissingRequiredField(message, out var agentId);
            if (missingfield && message.CurrentAgentId != agentId)
            {
                // Stack redirection agent
                _context.Push(agentId, reason: $"REDIRECTION {message.Content}");
            }
        }

        message.CurrentAgentId = _context.GetCurrentAgentId();

        return true;
    }
}
