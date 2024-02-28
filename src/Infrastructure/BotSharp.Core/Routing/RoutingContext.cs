using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing;

public class RoutingContext : IRoutingContext
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
            var preAgentId = _stack.Count == 0 ? agentId : _stack.Peek();
            _stack.Push(agentId);

            HookEmitter.Emit<IRoutingHook>(_services, async hook =>
                await hook.OnAgentEnqueued(agentId, preAgentId)
            ).Wait();
        }
    }

    /// <summary>
    /// Pop current agent
    /// </summary>
    public void Pop()
    {
        if (_stack.Count == 0)
        {
            return;
        }

        var agentId = _stack.Pop();

        HookEmitter.Emit<IRoutingHook>(_services, async hook =>
            await hook.OnAgentDequeued(agentId, _stack.Peek())
        ).Wait();
    }

    public void Replace(string agentId)
    {
        var fromAgent = agentId;
        var toAgent = agentId;

        if (_stack.Count == 0)
        {
            _stack.Push(agentId);
        }
        else if (_stack.Peek() != agentId)
        {
            fromAgent = _stack.Peek();
            _stack.Pop();
            _stack.Push(agentId);

            HookEmitter.Emit<IRoutingHook>(_services, async hook =>
                await hook.OnAgentReplaced(fromAgent, toAgent)
            ).Wait();
        }
    }

    public void Empty()
    {
        if (_stack.Count == 0)
        {
            return;
        }

        var agentId = GetCurrentAgentId();
        _stack.Clear();
        HookEmitter.Emit<IRoutingHook>(_services, async hook =>
            await hook.OnAgentQueueEmptied(agentId)
        ).Wait();
    }
}
