namespace BotSharp.Abstraction.Routing;

public interface IRoutingContext
{
    string GetCurrentAgentId();
    string FirstGoalAgentId();
    bool ContainsAgentId(string agentId);
    string OriginAgentId { get; }
    string EntryAgentId { get; }
    string ConversationId { get; }
    string MessageId { get; }
    void SetMessageId(string conversationId, string messageId);
    bool IsEmpty { get; }
    string IntentName { get; set; }
    int AgentCount { get; }
    void Push(string agentId, string? reason = null, bool updateLazyRouting = true);
    void Pop(string? reason = null, bool updateLazyRouting = true);
    void PopTo(string agentId, string reason, bool updateLazyRouting = true);
    void Replace(string agentId, string? reason = null, bool updateLazyRouting = true);
    void Empty(string? reason = null);


    int CurrentRecursionDepth { get; }
    int GetRecursiveCounter();
    void IncreaseRecursiveCounter();
    void SetRecursiveCounter(int counter);
    void ResetRecursiveCounter();

    Stack<string> GetAgentStack();
    void SetAgentStack(Stack<string> stack);
    void ResetAgentStack();

    void SetDialogs(List<RoleDialogModel> dialogs);
    List<RoleDialogModel> GetDialogs();
    void ResetDialogs();
}
