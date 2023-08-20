using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;

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
        var args = JsonSerializer.Deserialize<AgentRoutingArgs>(message.FunctionArgs);

        if (string.IsNullOrEmpty(args.AgentId))
        {
            var result = new FunctionExecutionValidationResult("false", "agent_id can't be parsed.");
            message.ExecutionResult = JsonSerializer.Serialize(result);
        }
        else
        {
            var result = new FunctionExecutionValidationResult("true");
            message.ExecutionResult = JsonSerializer.Serialize(result);
            message.CurrentAgentId = args.AgentId;
        }

        return true;
    }
}
