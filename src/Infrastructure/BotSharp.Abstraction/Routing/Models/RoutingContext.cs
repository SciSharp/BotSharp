using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingContext
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _setting;
    private string[] _routerAgentIds;

    public RoutingContext(IServiceProvider services, RoutingSettings setting)
    {
        _services = services;
        _setting = setting;
    }

    private Stack<string> _stack { get; set; }
        = new Stack<string>();

    /// <summary>
    /// Intent name
    /// </summary>
    public string IntentName { get; set; }

    /// <summary>
    /// Agent that can handle user original goal.
    /// </summary>
    public string OriginAgentId
    {
        get
        {
            if (_routerAgentIds == null)
            {
                var agentService = _services.GetRequiredService<IAgentService>();
                _routerAgentIds = agentService.GetAgents(new AgentFilter
                {
                    Type = AgentType.Routing
                }).Result.Items
                .Select(x => x.Id).ToArray();
            }

            return _stack.Where(x => !_routerAgentIds.Contains(x)).Last();
        }
    }

    public bool IsEmpty => !_stack.Any();
    public string GetCurrentAgentId()
    {
        return _stack.Peek();
    }

    public void Push(string agentId)
    {
        if (_stack.Count == 0 || _stack.Peek() != agentId)
        {
            _stack.Push(agentId);
        }
    }

    /// <summary>
    /// Pop current agent
    /// </summary>
    public void Pop()
    {
        _stack.Pop();
    }

    public void Empty()
    {
        _stack.Clear();
    }
}
