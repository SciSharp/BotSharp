namespace BotSharp.Abstraction.Routing.Models;

public class RoutingContext
{
    private Stack<string> _stack { get; set; }
        = new Stack<string>();

    /// <summary>
    /// Intent name
    /// </summary>
    public string IntentName { get; set; }

    public string OriginAgentId
        => _stack.Last();

    public string CurrentAgentId 
        => _stack.Peek();

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
    /// <returns>Return next agent</returns>
    public string Pop()
    {
        if (_stack.Count > 1)
        {
            _stack.Pop();
        }

        return _stack.Peek();
    }
}
