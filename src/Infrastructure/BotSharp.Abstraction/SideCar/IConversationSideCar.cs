using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.SideCar.Options;

namespace BotSharp.Abstraction.SideCar;

public interface IConversationSideCar
{
    string Provider { get; }
    bool IsEnabled { get; }

    Task AppendConversationDialogs(string conversationId, List<DialogElement> messages);
    Task<List<DialogElement>> GetConversationDialogs(string conversationId, ConversationDialogFilter? filter = null);
    Task UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint);
    Task<ConversationBreakpoint?> GetConversationBreakpoint(string conversationId);
    Task UpdateConversationStates(string conversationId, List<StateKeyValue> states);
    Task<RoleDialogModel> SendMessage(string agentId, string text,
        PostbackMessageModel? postback = null,
        List<MessageState>? states = null,
        List<DialogElement>? dialogs = null,
        SideCarOptions? options = null);
}
