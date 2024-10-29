namespace BotSharp.Abstraction.Routing;

public interface IRoutingContext
{
    string GetCurrentAgentId();
    string FirstGoalAgentId();
    bool ContainsAgentId(string agentId);
    string OriginAgentId { get; }
    string ConversationId { get; }
    string MessageId { get; }
    void SetMessageId(string conversationId, string messageId);
    bool IsEmpty { get; }
    string IntentName { get; set; }
    int AgentCount { get; }
    void Push(string agentId, string? reason = null);
    void Pop(string? reason = null);
    void PopTo(string agentId, string reason);
    void Replace(string agentId, string? reason = null);
    void Empty(string? reason = null);


    int CurrentRecursionDepth { get; }
    int GetRecursiveCounter();
    int IncreaseRecursiveCounter();
    void SetRecursiveCounter(int counter);
    void ResetRecursiveCounter();

    Stack<string> GetAgentStack();
    void SetAgentStack(Stack<string> stack);
    void ResetAgentStack();
}
