using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// Conversation state service to track the context in the conversation lifecycle
/// </summary>
public interface IConversationStateService
{
    void SetConversation(string conversationId);
    ConversationState Load();
    string GetState(string name);
    void SetState(string name, string value);
    void Save();
}
