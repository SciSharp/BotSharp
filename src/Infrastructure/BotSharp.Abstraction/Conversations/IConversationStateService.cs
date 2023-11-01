using System.Text.Json;

namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// Conversation state service to track the context in the conversation lifecycle
/// </summary>
public interface IConversationStateService
{
    string GetConversationId();
    ConversationState Load(string conversationId);
    string GetState(string name, string defaultValue = "");
    bool ContainsState(string name);
    ConversationState GetStates();
    IConversationStateService SetState<T>(string name, T value);
    void SaveStateByArgs(JsonDocument args);
    void CleanState();
    void Save();
}
