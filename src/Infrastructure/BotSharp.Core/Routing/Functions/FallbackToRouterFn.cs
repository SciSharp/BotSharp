using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing;

namespace BotSharp.Core.Routing.Functions;

public class FallbackToRouterFn : IFunctionCallback
{
    public string Name => "fallback_to_router";
    private readonly IServiceProvider _services;
    public FallbackToRouterFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<RoutingArgs>(message.FunctionArgs);
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = await agentService.GetAgents(new AgentFilter
        {
            AgentName = args.AgentName
        });
        var targetAgent = agents.Items.FirstOrDefault();
        if (targetAgent == null)
        {
            message.Content = $"Can't find routing agent {args.AgentName}";
            return false;
        }

        var routing = _services.GetRequiredService<RoutingContext>();
        routing.Replace(targetAgent.Id);

        var router = _services.GetRequiredService<IRoutingService>();
        message.CurrentAgentId = targetAgent.Id;
        var response = await router.InstructLoop(message);

        message.Content = response.Content;
        message.StopCompletion = true;

        return true;
    }
}
