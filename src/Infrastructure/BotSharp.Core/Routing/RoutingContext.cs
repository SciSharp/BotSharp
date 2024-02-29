using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing;

public class RoutingContext : IRoutingContext
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _setting;
    private string[] _routerAgentIds;
    private string _conversationId;
    private string _messageId;

    public RoutingContext(IServiceProvider services, RoutingSettings setting)
    {
        _services = services;
        _setting = setting;
    }

    public int AgentCount => _stack.Count;
    public string ConversationId => _conversationId;
    public string MessageId => _messageId;

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

    public void Push(string agentId, string? reason = null)
    {
        if (_stack.Count == 0 || _stack.Peek() != agentId)
        {
            var preAgentId = _stack.Count == 0 ? agentId : _stack.Peek();
            _stack.Push(agentId);

            HookEmitter.Emit<IRoutingHook>(_services, async hook =>
                await hook.OnAgentEnqueued(agentId, preAgentId, reason: reason)
            ).Wait();
        }
    }

    /// <summary>
    /// Pop current agent
    /// </summary>
    public void Pop(string? reason = null)
    {
        if (_stack.Count == 0)
        {
            return;
        }

        var agentId = _stack.Pop();
        var currentAgentId = GetCurrentAgentId();

        HookEmitter.Emit<IRoutingHook>(_services, async hook =>
            await hook.OnAgentDequeued(agentId, currentAgentId, reason: reason)
        ).Wait();
    }

    public string PreviousAgentId()
    {
        if (_stack.Count == 1)
        {
            return GetCurrentAgentId();
        }
        else if (_stack.Count > 1)
        {
            return _stack.ToArray()[1];
        }

        return string.Empty;
    }

    public void Replace(string agentId, string? reason = null)
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
                await hook.OnAgentReplaced(fromAgent, toAgent, reason: reason)
            ).Wait();
        }
    }

    public void Empty(string? reason = null)
    {
        if (_stack.Count == 0)
        {
            return;
        }

        var agentId = GetCurrentAgentId();
        _stack.Clear();
        HookEmitter.Emit<IRoutingHook>(_services, async hook =>
            await hook.OnAgentQueueEmptied(agentId, reason: reason)
        ).Wait();
    }

    public void SetMessageId(string conversationId, string messageId)
    {
        _conversationId = conversationId;
        _messageId = messageId;
    }
}
