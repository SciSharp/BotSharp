using System.Text.Json;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.Google.Core
{
    public class NullConversationStateService:IConversationStateService
    {
        public void Dispose()
        {
            // TODO release managed resources here
        }

        public string GetConversationId()
        {
            return "fake-conversation-id";
        }

        public Dictionary<string, string> Load(string conversationId, bool isReadOnly = false)
        {
            return new Dictionary<string, string> { { "Key", "Value" } };
        }

        public string GetState(string name, string defaultValue = "")
        {
            var states = GetStates();
            if (!states.ContainsKey(name))
                return defaultValue;
            return states[name]??defaultValue;
        }

        public bool ContainsState(string name)
        {
            return false;
        }

        public Dictionary<string, string> GetStates()
        {
            return new Dictionary<string, string> { { "temperature", "0.5" }, { "max_tokens", "8000" }, { "top_p", "1.0" }, { "frequency_penalty", "0.0" } };
        }

        public IConversationStateService SetState<T>(string name, T value, bool isNeedVersion = true, int activeRounds = -1,
            string valueType = StateDataType.String, string source = StateSource.User, bool readOnly = false)
        {
            return this;
        }

        public void SaveStateByArgs(JsonDocument args)
        {
           
        }

        public bool RemoveState(string name)
        {
            return true;
        }

        public void CleanStates(params string[] excludedStates)
        {
           
        }

        public void Save()
        {
           
        }

        public ConversationState GetCurrentState()
        {
           
            return new ConversationState { { "StateKey", new StateKeyValue { Key = "Key", Values = new List<StateValue>()} } };
        }

        public void SetCurrentState(ConversationState state)
        {
        }

           
        public void ResetCurrentState()
        {
           
        }
    }
}