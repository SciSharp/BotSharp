using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing;

public class RoutingContext : IRoutingContext
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _setting;
    private string[] _routerAgentIds;
    private string _conversationId;
    private string _messageId;
    private int _currentRecursionDepth = 0;

    public RoutingContext(IServiceProvider services, RoutingSettings setting)
    {
        _services = services;
        _setting = setting;
    }

    public int AgentCount => _stack.Count;
    public string ConversationId => _conversationId;
    public string MessageId => _messageId;
    public int CurrentRecursionDepth => _currentRecursionDepth;

    private Stack<string> _stack { get; set; } = new();

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
                    Type = AgentType.Routing,
                    Pager = new Pagination { Size = 20 }
                }).Result.Items.Select(x => x.Id).ToArray();
            }

            return _stack.Where(x => !_routerAgentIds.Contains(x)).LastOrDefault() ?? string.Empty;
        }
    }

    /// <summary>
    /// Entry agent
    /// </summary>
    public string EntryAgentId
    {
        get
        {
            return _stack.LastOrDefault() ?? string.Empty;
        }
    }

    public bool IsEmpty => !_stack.Any();

    public string GetCurrentAgentId()
    {
        if (_stack.Count == 0)
        {
            return string.Empty;
        }

        return _stack.Peek();
    }

    /// <summary>
    /// Push agent
    /// </summary>
    /// <param name="agentId">Id or Name</param>
    /// <param name="reason"></param>
    public void Push(string agentId, string? reason = null)
    {
        // Convert id to name
        if (!Guid.TryParse(agentId, out _))
        {
            var agentService = _services.GetRequiredService<IAgentService>();
            agentId = agentService.GetAgents(new AgentFilter
            {
                AgentName = agentId
            }).Result.Items.First().Id;
        }

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

        if (string.IsNullOrEmpty(currentAgentId))
        {
            return;
        }

        // Run the routing rule
        var agency = _services.GetRequiredService<IAgentService>();
        var agent = agency.LoadAgent(currentAgentId).Result;

        var message = new RoleDialogModel(AgentRole.User, $"Try to route to agent {agent.Name}")
        {
            CurrentAgentId = currentAgentId,
            FunctionName = "route_to_agent",
            FunctionArgs = JsonSerializer.Serialize(new FunctionCallFromLlm
            {
                Function = "route_to_agent",
                AgentName = agent.Name,
                NextActionReason = $"User manually route to agent {agent.Name}"
            })
        };

        var routing = _services.GetRequiredService<IRoutingService>();
        var (missingfield, _) = routing.HasMissingRequiredField(message, out agentId);
        if (missingfield)
        {
            if (currentAgentId != agentId)
            {
                _stack.Push(agentId);
            }
        }
    }

    public void PopTo(string agentId, string reason)
    {
        var currentAgentId = GetCurrentAgentId();
        while (!string.IsNullOrEmpty(currentAgentId) && 
            currentAgentId != agentId)
        {
            Pop(reason);
            currentAgentId = GetCurrentAgentId();
        }
    }

    public string FirstGoalAgentId()
    {
        if (_stack.Count == 1)
        {
            return GetCurrentAgentId();
        }
        else if (_stack.Count > 1)
        {
            return _stack.ToArray()[_stack.Count - 2];
        }

        return string.Empty;
    }

    public bool ContainsAgentId(string agentId)
    {
        return _stack.ToArray().Contains(agentId);
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

    public int GetRecursiveCounter()
    {
        return _currentRecursionDepth;
    }

    public void IncreaseRecursiveCounter()
    {
        _currentRecursionDepth++;
    }

    public void SetRecursiveCounter(int counter)
    {
        _currentRecursionDepth = counter;
    }

    public void ResetRecursiveCounter()
    {
        _currentRecursionDepth = 0;
    }

    public Stack<string> GetAgentStack()
    {
        return new Stack<string>(_stack);
    }

    public void SetAgentStack(Stack<string> stack)
    {
        _stack = new Stack<string>(stack);
    }

    public void ResetAgentStack()
    {
        _stack.Clear();
    }
}
