using System.Text.Json;

namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// Conversation state service to track the context in the conversation lifecycle
/// </summary>
public interface IConversationStateService
{
    string GetConversationId();
    Dictionary<string, string> Load(string conversationId);
    string GetState(string name, string defaultValue = "");
    bool ContainsState(string name);
    Dictionary<string, string> GetStates();
    IConversationStateService SetState<T>(string name, T value, bool isNeedVersion = true, int activeRounds = -1);
    void SaveStateByArgs(JsonDocument args);
    void CleanStates();
    void Save();
}
