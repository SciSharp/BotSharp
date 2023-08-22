using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;
using Microsoft.Extensions.Logging;
using System.IO;

namespace BotSharp.Core.Functions;

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
        var result = new RoutingResult($"Routed to {args.AgentName}");

        if (string.IsNullOrEmpty(args.AgentName))
        {
            result = new RoutingResult($"Can't find {args.AgentName}");
        }
        else
        {
            var agentSettings = _services.GetRequiredService<AgentSettings>();
            var dbSettings = _services.GetRequiredService<MyDatabaseSettings>();
            var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir, agentSettings.RouterId, "route.json");
            var routes = JsonSerializer.Deserialize<RoutingTable[]>(File.ReadAllText(filePath));

            var agent = routes.FirstOrDefault(x => x.AgentName.ToLower() == args.AgentName.ToLower());
            if (agent == null)
            {
                result = new RoutingResult($"Can't find agent {args.AgentName}.");
            }
            else
            {
                // Check required fields
                var jo = JsonSerializer.Deserialize<object>(message.FunctionArgs);
                bool hasMissingField = false;
                foreach (var field in agent.RequiredFields)
                {
                    if (jo is JsonElement root)
                    {
                        if (!root.EnumerateObject().Any(x => x.Name == field))
                        {
                            result = new RoutingResult($"Please provide {field}.");
                            hasMissingField = true;
                            break;
                        }
                    }
                }

                if (!hasMissingField)
                {
                    message.CurrentAgentId = agent.AgentId;
                }
            }
        }

        message.ExecutionResult = JsonSerializer.Serialize(result);
        return true;
    }
}
