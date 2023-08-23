using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using System.IO;

namespace BotSharp.Core.Functions;

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

        var routes = GetRoutingTable();
        var routingRule = routes.FirstOrDefault(x => x.AgentName.ToLower() == args.AgentName.ToLower());

        if (routingRule == null)
        {
            agentId = message.CurrentAgentId;
            message.ExecutionResult = $"Can't find agent {args.AgentName}";
            return true;
        }

        agentId = routingRule.AgentId;

        // Check required fields
        var jo = JsonSerializer.Deserialize<object>(message.FunctionArgs);
        bool hasMissingField = false;
        foreach (var field in routingRule.RequiredFields)
        {
            if (jo is JsonElement root)
            {
                if (!root.EnumerateObject().Any(x => x.Name == field))
                {
                    message.ExecutionResult = $"missing {field}.";
                    hasMissingField = true;
                    break;
                }
                else if (root.EnumerateObject().Any(x => x.Name == field) && 
                    string.IsNullOrEmpty(root.EnumerateObject().FirstOrDefault(x => x.Name == field).Value.ToString()))
                {
                    message.ExecutionResult = $"missing {field}.";
                    hasMissingField = true;
                    break;
                }
            }
        }

        if (hasMissingField && !string.IsNullOrEmpty(routingRule.RedirectTo))
        {
            agentId = routingRule.RedirectTo;
        }

        return hasMissingField;
    }

    private RoutingTable[] GetRoutingTable()
    {
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var dbSettings = _services.GetRequiredService<MyDatabaseSettings>();
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir, agentSettings.RouterId, "route.json");
        return JsonSerializer.Deserialize<RoutingTable[]>(File.ReadAllText(filePath));
    }
}
