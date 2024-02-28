namespace BotSharp.Abstraction.Routing;

public interface IRoutingContext
{
    string GetCurrentAgentId();
    string PreviousAgentId();
    string OriginAgentId { get; }
    string ConversationId { get; }
    string MessageId { get; }
    void SetMessageId(string conversationId, string messageId);
    bool IsEmpty { get; }
    string IntentName { get; set; }
    int AgentCount { get; }
    void Push(string agentId, string? reason = null);
    void Pop(string? reason = null);
    void Replace(string agentId, string? reason = null);
    void Empty(string? reason = null);
}
