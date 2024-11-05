using BotSharp.Abstraction.Conversations.Enums;
using System.Text.Json;

namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// Conversation state service to track the context in the conversation lifecycle
/// </summary>
public interface IConversationStateService
{
    string GetConversationId();
    Dictionary<string, string> Load(string conversationId, bool isReadOnly = false);
    string GetState(string name, string defaultValue = "");
    bool ContainsState(string name);
    Dictionary<string, string> GetStates();
    IConversationStateService SetState<T>(string name, T value, bool isNeedVersion = true,
        int activeRounds = -1, string valueType = StateDataType.String, string source = StateSource.User, bool readOnly = false);
    void SaveStateByArgs(JsonDocument args);
    bool RemoveState(string name);
    void CleanStates(params string[] excludedStates);
    void Save();

    ConversationState GetCurrentState();
    void SetCurrentState(ConversationState state);
    void ResetCurrentState();
}
