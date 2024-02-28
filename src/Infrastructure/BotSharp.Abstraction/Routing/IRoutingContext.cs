namespace BotSharp.Abstraction.Routing;

public interface IRoutingContext
{
    string GetCurrentAgentId();
    string OriginAgentId { get; }
    bool IsEmpty { get; }
    string IntentName { get; set; }
    void Push(string agentId);
    void Pop();
    void Replace(string agentId);
    void Empty();
}
