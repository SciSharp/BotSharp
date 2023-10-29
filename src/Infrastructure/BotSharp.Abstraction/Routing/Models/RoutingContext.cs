using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingContext
{
    private readonly RoutingSettings _setting;
    public RoutingContext(RoutingSettings setting)
    {
        _setting = setting;
    }

    private Stack<string> _stack { get; set; }
        = new Stack<string>();

    /// <summary>
    /// Intent name
    /// </summary>
    public string IntentName { get; set; }

    /// <summary>
    /// Agent that can handl user original goal.
    /// </summary>
    public string OriginAgentId
        => _stack.Where(x => x != _setting.RouterId).Last();

    public bool IsEmpty => !_stack.Any();
    public string GetCurrentAgentId()
    {
        if (_stack.Count == 0)
        {
            _stack.Push(_setting.RouterId);
        }
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
