using System.Collections.Concurrent;

namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationState : ConcurrentDictionary<string, StateKeyValue>
{
    public ConversationState()
    {

    }

    public ConversationState(List<StateKeyValue> pairs)
    {
        foreach (var pair in pairs)
        {
            this[pair.Key] = pair;
        }
    }
}
