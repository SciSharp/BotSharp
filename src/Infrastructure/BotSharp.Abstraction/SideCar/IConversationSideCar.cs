namespace BotSharp.Abstraction.SideCar;

public interface IConversationSideCar
{
    string Provider { get; }

    bool IsEnabled();
    void AppendConversationDialogs(string conversationId, List<DialogElement> messages);
    List<DialogElement> GetConversationDialogs(string conversationId);
    void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint);
    ConversationBreakpoint? GetConversationBreakpoint(string conversationId);
    Task<RoleDialogModel> SendMessage(string agentId, string text,
        PostbackMessageModel? postback = null, List<MessageState>? states = null, List<DialogElement>? dialogs = null);
}
