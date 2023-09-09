using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Core.Routing;

/// <summary>
/// Router calls this function to set the Active Agent according to the context
/// </summary>
public class RouteToAgentFn : IFunctionCallback
{
    public string Name => "route_to_agent";
    private readonly IServiceProvider _services;

    public RouteToAgentFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<RoutingArgs>(message.FunctionArgs);

        if (string.IsNullOrEmpty(args.AgentName))
        {
            message.ExecutionResult = $"missing agent name";
        }
        else
        {
            var missingfield = HasMissingRequiredField(message, out var agentId);
            if (missingfield && message.CurrentAgentId != agentId)
            {
                message.CurrentAgentId = agentId;
            }
            else
            {
                message.CurrentAgentId = agentId;
                message.ExecutionResult = $"Routed to {args.AgentName}";
            }
        }

        return true;
    }

    /// <summary>
    /// If the target agent needs some required fields but the
    /// </summary>
    /// <returns></returns>
    private bool HasMissingRequiredField(RoleDialogModel message, out string agentId)
    {
        var args = JsonSerializer.Deserialize<RoutingArgs>(message.FunctionArgs);
        var router = _services.GetRequiredService<IAgentRouting>();
        var routingRule = router.GetRecordByName(args.AgentName);

        if (routingRule == null)
        {
            agentId = message.CurrentAgentId;
            message.ExecutionResult = $"Can't find agent {args.AgentName}";
            return true;
        }

        agentId = routingRule.AgentId;

        // Check required fields
        var root = JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs);
        bool hasMissingField = false;
        string missingFieldName = "";
        foreach (var field in routingRule.RequiredFields)
        {
            if (!root.EnumerateObject().Any(x => x.Name == field))
            {
                message.ExecutionResult = $"missing {field}.";
                hasMissingField = true;
                missingFieldName = field;
                break;
            }
            else if (root.EnumerateObject().Any(x => x.Name == field) &&
                string.IsNullOrEmpty(root.EnumerateObject().FirstOrDefault(x => x.Name == field).Value.ToString()))
            {
                message.ExecutionResult = $"missing {field}.";
                hasMissingField = true;
                missingFieldName = field;
                break;
            }
        }

        // Check if states contains the field according conversation context.
        var states = _services.GetRequiredService<IConversationStateService>();
        if (!string.IsNullOrEmpty(states.GetState(missingFieldName)))
        {
            var value = states.GetState(missingFieldName);
            message.FunctionArgs = message.FunctionArgs.Substring(0, message.FunctionArgs.Length - 1) + $", \"{missingFieldName}\": \"{value}\"" + "}";
            hasMissingField = false;
            missingFieldName = "";
        }

        if (hasMissingField && !string.IsNullOrEmpty(routingRule.RedirectTo))
        {
            agentId = routingRule.RedirectTo;
        }

        return hasMissingField;
    }
}
