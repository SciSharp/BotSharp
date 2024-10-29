namespace BotSharp.Abstraction.Conversations;

public interface IConversationSideCar
{
    bool IsEnabled();
    void AppendConversationDialogs(string conversationId, List<DialogElement> messages);
    List<DialogElement> GetConversationDialogs(string conversationId);
    void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint);
    ConversationBreakpoint? GetConversationBreakpoint(string conversationId);
    Task<RoleDialogModel> Execute(string conversationId, string agentId, string text, PostbackMessageModel? postback = null, List<MessageState>? states = null);
}
