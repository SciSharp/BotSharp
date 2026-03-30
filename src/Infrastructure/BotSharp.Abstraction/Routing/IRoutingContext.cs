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
    Task Push(string agentId, string? reason = null, bool updateLazyRouting = true);
    Task Pop(string? reason = null, bool updateLazyRouting = true);
    Task PopTo(string agentId, string reason, bool updateLazyRouting = true);
    Task Replace(string agentId, string? reason = null, bool updateLazyRouting = true);
    Task Empty(string? reason = null);


    int CurrentRecursionDepth { get; }
    int GetRecursiveCounter();
    void IncreaseRecursiveCounter();
    void SetRecursiveCounter(int counter);
    void ResetRecursiveCounter();

    Stack<string> GetAgentStack();
    void SetAgentStack(Stack<string> stack);
    void ResetAgentStack();

    void SetDialogs(List<RoleDialogModel> dialogs);
    void AddDialogs(List<RoleDialogModel> dialogs);
    List<RoleDialogModel> GetDialogs();
    void ResetDialogs();
}
